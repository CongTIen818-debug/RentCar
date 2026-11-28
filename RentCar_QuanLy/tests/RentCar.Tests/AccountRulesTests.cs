using RentCar.Application.Accounts;
using Xunit;

namespace RentCar.Tests;

/// <summary>
/// Kiểm thử quy tắc tài khoản dùng chung (YEU_CAU_TIEP_THEO mục 3.D): mã đăng ký nhân viên và mã đặt lại mật khẩu,
/// định dạng email, số điện thoại, mật khẩu và hạng GPLX. Các hàm đều thuần, không cần CSDL.
/// </summary>
public class AccountRulesTests
{
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    [Fact]
    public void NewCode_HasTwelveCharactersFromSafeAlphabet()
    {
        for (var i = 0; i < 50; i++)
        {
            var code = AccountRules.NewCode();
            Assert.Equal(12, code.Length);
            Assert.True(code.All(ch => CodeAlphabet.Contains(ch)));
        }
    }

    [Fact]
    public void NewCode_NeverUsesConfusableCharacters()
    {
        // Bỏ 0, 1, I, O để người dùng không nhầm khi chép mã.
        for (var i = 0; i < 50; i++)
            Assert.False(AccountRules.NewCode().Any(ch => "01IO".Contains(ch)));
    }

    [Fact]
    public void FormatCode_GroupsByFourWithDashes()
    {
        Assert.Equal("ABCD-EFGH-JKMN", AccountRules.FormatCode("abcdefghjkmn"));
    }

    [Fact]
    public void NormalizeCode_RemovesSeparatorsAndUppercases()
    {
        Assert.Equal("ABCD12", AccountRules.NormalizeCode(" ab-cd 12 "));
    }

    [Fact]
    public void HashCode_IgnoresFormattingAndCase()
    {
        Assert.Equal(AccountRules.HashCode("ABCD-EFGH-JKMN"), AccountRules.HashCode("abcd efgh jkmn"));
    }

    [Fact]
    public void HashCode_IsSha256HexAndNotPlainText()
    {
        var hash = AccountRules.HashCode("ABCD-EFGH-JKMN");
        Assert.Matches("^[0-9A-F]{64}$", hash);
        Assert.NotEqual("ABCDEFGHJKMN", hash);
    }

    [Fact]
    public void CodeMatches_AcceptsSameCodeInAnyFormat_AndRejectsOtherCode()
    {
        var stored = AccountRules.HashCode("ABCD-EFGH-JKMN");
        Assert.True(AccountRules.CodeMatches(stored, "abcd efgh jkmn"));
        Assert.False(AccountRules.CodeMatches(stored, "ABCD-EFGH-JKMP"));
    }

    [Theory]
    [InlineData("nv@rentcar.vn", true)]
    [InlineData("a@b.co", true)]
    [InlineData("khong-co-at", false)]
    [InlineData("a@b", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidEmail_AcceptsOnlyWellFormedAddresses(string? email, bool expected)
    {
        Assert.Equal(expected, AccountRules.IsValidEmail(email));
    }

    [Theory]
    [InlineData("0912345678", true)]
    [InlineData("09123456789", true)]
    [InlineData("0912345678901", false)]
    [InlineData("1912345678", false)]
    [InlineData("091234", false)]
    public void IsValidPhone_RequiresTenOrElevenDigitsStartingWithZero(string phone, bool expected)
    {
        Assert.Equal(expected, AccountRules.IsValidPhone(phone));
    }

    [Theory]
    [InlineData("Abcdef12", true)]
    [InlineData("abcdefg1", false)] // thiếu chữ hoa
    [InlineData("ABCDEFG1", false)] // thiếu chữ thường
    [InlineData("Abcdefgh", false)] // thiếu chữ số
    [InlineData("Ab1", false)]      // dưới 8 ký tự
    public void IsStrongPassword_NeedsLengthUpperLowerAndDigit(string password, bool expected)
    {
        Assert.Equal(expected, AccountRules.IsStrongPassword(password));
    }

    [Theory]
    [InlineData("B", true)]
    [InlineData(" c ", true)]
    [InlineData("e", true)]
    [InlineData("A", false)]
    [InlineData("F", false)]
    [InlineData("", false)]
    public void IsValidLicenseClass_OnlyBToE(string value, bool expected)
    {
        Assert.Equal(expected, AccountRules.IsValidLicenseClass(value));
    }
}
