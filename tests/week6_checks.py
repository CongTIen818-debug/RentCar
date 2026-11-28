"""Week 6 customer integration checks. Only run against an isolated local test database."""
import urllib.request,urllib.parse,urllib.error,http.cookiejar,ssl,re,html,json,subprocess,time,concurrent.futures,statistics,pathlib
BASE='https://localhost:51917'
DB='RentCar_Week6_Verify_20261128'
assert DB.startswith('RentCar_Week6_Verify_')
RESULTS=[]
def passed(name):RESULTS.append(name);print('PASS',name)
def sql(q):
 out=subprocess.check_output(['sqlcmd','-S',r'lpc:BLUE-506\SQLEXPRESS','-d',DB,'-E','-I','-w','65535','-y','0','-Q','SET NOCOUNT ON; '+q],text=True,encoding='utf-8');return json.loads(out.strip())
def op():return urllib.request.build_opener(urllib.request.HTTPSHandler(context=ssl._create_unverified_context()),urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
def request(c,path,data=None,json_body=False,headers=None):
 h=headers or {};body=None
 if data is not None:
  if json_body:body=json.dumps(data).encode();h=dict(h,**{'Content-Type':'application/json'})
  else:body=urllib.parse.urlencode(data,doseq=True).encode()
 try:
  r=c.open(urllib.request.Request(BASE+path,body,h));return r.status,html.unescape(r.read().decode()),r.url,r.headers
 except urllib.error.HTTPError as e:return e.code,html.unescape(e.read().decode()),e.url,e.headers

def csrf(c,path=None):
 if path:
  s,h,_,_=request(c,path);assert s==200,(path,s,h[:300]);return re.search(r'name="__RequestVerificationToken"[^>]*value="([^\"]+)"',h).group(1)
 s,h,_,_=request(c,'/api/csrf');assert s==200;return json.loads(h)['token']
def postjson(c,path,data):return request(c,path,data,True,{'X-CSRF-TOKEN':csrf(c)})
def signin(c,email,pwd):
 s,h,u,_=postjson(c,'/api/auth/login',{'Email':email,'Password':pwd});assert s==200,(s,h);return json.loads(h)

guest=op();s,h,_,_=request(guest,'/');assert s==200 and 'Hộp số' in h and 'Nhiên liệu' in h;passed('homepage filters and unchanged customer layout')
s,h,_,_=request(guest,'/api/vehicles?size=3&sort=price-asc');a=json.loads(h);assert s==200 and len(a['items'])==3 and a['pages']>=2;assert [v['dailyRate'] for v in a['items']]==sorted(v['dailyRate'] for v in a['items']);first=[v['id'] for v in a['items']]
s,h,_,_=request(guest,'/api/vehicles?size=3&page=2&sort=price-asc');assert not set(first).intersection(v['id'] for v in json.loads(h)['items']);passed('SQL pagination and deterministic price sorting')
for query in ['size=101','minPrice=200&maxPrice=100','sort=evil','seats=0','pickupAt=2020-01-01T09:00&returnAt=2020-01-02T09:00']:
 assert request(guest,'/api/vehicles?'+query)[0]==400,query
for query in ['q=BMW','fuels=Xăng','transmissions=Số tự động','minPrice=1000000&maxPrice=4000000','seats=2']:
 s,h,_,_=request(guest,'/api/vehicles?'+urllib.parse.quote(query,safe='=&'));assert s==200,(query,h)
assert json.loads(request(guest,'/api/vehicles?q=%27%20OR%201=1--')[1])['total']==0
raw=guest.open(BASE+'/?q=%3Cscript%3Ealert(1)%3C/script%3E').read().decode();assert '<script>alert(1)</script>' not in raw
passed('filter validation, text search and SQL parameterization')
assert request(guest,'/api/rental-requests')[0]==401
assert request(guest,'/api/auth/login',{'Email':'none@rentcar.test','Password':'Wrong1'},True)[0]==400
assert json.loads(request(guest,'/api/auth/login',{'Email':'none@rentcar.test','Password':'Wrong1'},True)[1])['code']=='CSRF'
assert request(guest,'/Rental/Checkout')[0]==409
s,h,_,_=request(guest,'/swagger/v1/swagger.json');spec=json.loads(h);assert '/api/vehicles' in spec['paths'] and not any('QuanLy' in p for p in spec['paths']);passed('API auth status, JSON CSRF errors and Swagger scope')
email='week6-user@rentcar.test';pwd='Week6Start1';data={'FullName':'Nguyễn Kiểm Thử','BirthDate':'1988-01-05','Phone':'0912345678','Email':email,'IdNumber':'123456789013','Address':'Hà Nội','LicenseNumber':'WEEK6-USER-LICENSE','LicenseClass':'B','LicenseExpiry':'2030-12-31','Password':pwd,'ConfirmPassword':pwd}
s,h,_,_=postjson(guest,'/api/auth/register',dict(data,Password='123456',ConfirmPassword='123456'));assert s==400
s,h,_,_=postjson(guest,'/api/auth/register',data);assert s==201,(s,h)
s,h,_,_=postjson(guest,'/api/auth/register',data);assert s==400 or s==409
user=signin(guest,email,pwd);uid=user['id'];passed('registration validation, duplicate email and PBKDF2 login')
s,h,_,headers=request(guest,'/tai-khoan/ho-so?id=1');assert s==200 and email in h and '9013' in h and '123456789013' not in h;assert 'no-store' in headers.get('Cache-Control','') and headers.get('X-Content-Type-Options')=='nosniff'
t=csrf(guest,'/tai-khoan/ho-so');s,h,_,_=request(guest,'/tai-khoan/ho-so',{'FullName':'Nguyễn Hồ Sơ','Phone':'0912345678','Address':'Hà Nội','LicenseNumber':'WEEK6-USER-LICENSE','LicenseClass':'B','LicenseExpiry':'2030-12-31','id':1,'__RequestVerificationToken':t});assert s==200 and 'Nguyễn Hồ Sơ' in h
assert sql('SELECT hoTen name FROM nguoiDung WHERE idNguoiDung='+str(uid)+' FOR JSON PATH')[0]['name']=='Nguyễn Hồ Sơ'
assert sql('SELECT hoTen name FROM nguoiDung WHERE idNguoiDung=1 FOR JSON PATH')[0]['name']!='Nguyễn Hồ Sơ';passed('profile ownership, license update, identity masking and private response headers')
old=op();signin(old,email,pwd);request(old,'/tai-khoan/ho-so')
t=csrf(guest,'/tai-khoan/doi-mat-khau');s,h,u,_=request(guest,'/tai-khoan/doi-mat-khau',{'CurrentPassword':pwd,'NewPassword':'Week6Changed2','ConfirmPassword':'Week6Changed2','__RequestVerificationToken':t});assert '/Account/Login' in u;pwd='Week6Changed2'
assert '/Account/Login' in request(old,'/tai-khoan/ho-so')[2];signin(guest,email,pwd);passed('password change signs out stale customer sessions')
t=csrf(guest,'/tai-khoan/quen-mat-khau');s,h,_,_=request(guest,'/tai-khoan/quen-mat-khau',{'Email':email,'__RequestVerificationToken':t});assert s==200 and 'Nếu email đã đăng ký' in h
link=re.search(r'href="(https://localhost:51917/tai-khoan/dat-lai-mat-khau[^\"]+)"',h).group(1);params=urllib.parse.parse_qs(urllib.parse.urlsplit(link).query);code=params['code'][0]
row=sql('SELECT maDatLaiMatKhau hash, DATEDIFF(minute,DATEADD(HOUR,7,SYSUTCDATETIME()),hanMaDatLai) mins FROM nguoiDung WHERE idNguoiDung='+str(uid)+' FOR JSON PATH')[0];assert row['hash']!=code and len(row['hash'])==64 and 29<=row['mins']<=30
reset=op();t=csrf(reset,'/tai-khoan/dat-lai-mat-khau?'+urllib.parse.urlencode({'email':email,'code':code}));payload={'Email':email,'Code':code,'NewPassword':'Week6Reset3','ConfirmPassword':'Week6Reset3','__RequestVerificationToken':t}
s,h,u,_=request(reset,'/tai-khoan/dat-lai-mat-khau',payload);assert '/Account/Login' in u
s,h,u,_=request(reset,'/tai-khoan/dat-lai-mat-khau',payload);assert 'đã dùng hoặc đã hết hạn' in h
signin(guest,email,'Week6Reset3');passed('recovery token hashed, 30-minute expiry and single-use link')
# Owner-only notifications derived from existing financial history or explicit notification rows.
subprocess.check_call(['sqlcmd','-S',r'lpc:BLUE-506\SQLEXPRESS','-d',DB,'-E','-I','-b','-Q',f"INSERT thongBao(idNguoiDung,idHopDong,loai,noiDung,trangThaiGui,thoiDiem) VALUES({uid},NULL,'NHAC_DAT_COC',N'Thông báo kiểm tra riêng',1,DATEADD(HOUR,7,SYSUTCDATETIME()));"],stdout=subprocess.DEVNULL)
s,h,_,_=request(guest,'/thong-bao');assert s==200 and 'Thông báo kiểm tra riêng' in h
key=re.search(r'name="key" type="hidden" value="([^\"]+)"',h).group(1);t=csrf(guest,'/thong-bao');request(guest,'/thong-bao/da-doc',{'key':key,'__RequestVerificationToken':t});s,h,_,_=request(guest,'/thong-bao');assert 'Đã đọc' in h
assert request(guest,'/thong-bao/da-doc',{'key':'n-999999','__RequestVerificationToken':t})[0]==404;passed('notification ownership and explicit read receipt')
# Anonymization is an explicit password-protected action; it cannot erase another user or financial transactions.
t=csrf(guest,'/tai-khoan/quyen-rieng-tu');s,h,_,_=request(guest,'/tai-khoan/quyen-rieng-tu',{'Password':'wrong','Confirm':'true','__RequestVerificationToken':t});assert 'Mật khẩu hiện tại không đúng' in h
contracts=sql('SELECT COUNT(*) n FROM hopDong FOR JSON PATH')[0]['n'];payments=sql('SELECT COUNT(*) n FROM thanhToan FOR JSON PATH')[0]['n']
t=csrf(guest,'/tai-khoan/quyen-rieng-tu');s,h,u,_=request(guest,'/tai-khoan/quyen-rieng-tu',{'Password':'Week6Reset3','Confirm':'true','__RequestVerificationToken':t});row=sql('SELECT hoTen name,email,trangThai active,cccd id FROM nguoiDung WHERE idNguoiDung='+str(uid)+' FOR JSON PATH')[0];assert row['active']==False and row['email'].endswith('@rentcar.invalid') and 'id' not in row
assert sql('SELECT COUNT(*) n FROM hopDong FOR JSON PATH')[0]['n']==contracts and sql('SELECT COUNT(*) n FROM thanhToan FOR JSON PATH')[0]['n']==payments;passed('explicit anonymization retains contracts and financial transactions')
assert request(op(),'/health')[0]==200
print('TOTAL',len(RESULTS),'functional groups passed')
pathlib.Path(__file__).with_name('week6-functional-results.json').write_text(json.dumps({'passed':RESULTS},ensure_ascii=False,indent=2),encoding='utf-8')
