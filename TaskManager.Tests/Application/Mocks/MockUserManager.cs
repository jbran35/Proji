using Microsoft.AspNetCore.Identity;
using Moq;
using TaskManager.Domain.Entities;

namespace TaskManager.Tests.Application.Mocks
{
    public static class MockUserManager
    {
        public static Mock<UserManager<User>> GetMockUserManager()
        {
            var store = new Mock<IUserStore<User>>();
            var mgr = new Mock<UserManager<User>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
            mgr.Object.UserValidators.Add(new UserValidator<User>());
            mgr.Object.PasswordValidators.Add(new PasswordValidator<User>());
            return mgr;
        }
    }
}