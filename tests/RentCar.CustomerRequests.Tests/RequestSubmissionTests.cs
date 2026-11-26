using RentCar.Services;
using RentCar.Domain.Common;
using Xunit;

public class RequestSubmissionTests
{
    private static readonly DateTime Now = new(2026,11,26,10,0,0);
    [Fact] public void PickupTomorrowIsAllowed() => CustomerRequestSubmission.ValidateSchedule(new(2026,11,27,0,0,0),new(2026,11,27,4,0,0),Now);
    [Fact] public void LaterTodayIsRejected() => Assert.Throws<DomainException>(()=>CustomerRequestSubmission.ValidateSchedule(new(2026,11,26,20,0,0),new(2026,11,27,20,0,0),Now));
    [Fact] public void ReversedWindowIsRejected() => Assert.Throws<DomainException>(()=>CustomerRequestSubmission.ValidateSchedule(new(2026,11,28),new(2026,11,27),Now));
    [Fact] public void LessThanFourHoursIsRejected() => Assert.Throws<DomainException>(()=>CustomerRequestSubmission.ValidateSchedule(new(2026,11,27,9,0,0),new(2026,11,27,12,59,0),Now));
    [Fact] public void MoreThanThirtyDaysIsRejected() => Assert.Throws<DomainException>(()=>CustomerRequestSubmission.ValidateSchedule(new(2026,11,27),new(2026,12,27,0,1,0),Now));
    [Fact] public void ExactlyThirtyDaysIsAllowed() => CustomerRequestSubmission.ValidateSchedule(new(2026,11,27),new(2026,12,27),Now);
}
