using System.Threading;
using PasswordGenerator.ViewModels;
using Xunit;

namespace PasswordGenerator.Tests
{
    public class ViewModelTests
    {
        [Fact]
        public void ViewModel_InitialState_Generates16CharPassword()
        {
            var vm = new MainViewModel();

            Assert.Equal(16, vm.PasswordLength);
            Assert.True(vm.HasPassword);
            Assert.Equal(16, vm.GeneratedPassword.Length);
            Assert.True(vm.IsDarkMode);
            Assert.Equal("☀️", vm.ThemeIcon);
        }

        [Fact]
        public void ViewModel_ChangeLength_RegeneratesPasswordWithNewLength()
        {
            var vm = new MainViewModel();

            vm.PasswordLength = 24;
            Assert.Equal(24, vm.GeneratedPassword.Length);

            vm.PasswordLength = 8;
            Assert.Equal(8, vm.GeneratedPassword.Length);
        }

        [Fact]
        public void ViewModel_TogglePasswordVisibility_ChangesDisplayedPassword()
        {
            var vm = new MainViewModel();
            string original = vm.GeneratedPassword;

            Assert.Equal(original, vm.DisplayedPassword);

            vm.TogglePasswordVisibilityCommand.Execute(null);

            Assert.True(vm.IsPasswordHidden);
            Assert.Equal(new string('●', original.Length), vm.DisplayedPassword);

            vm.TogglePasswordVisibilityCommand.Execute(null);
            Assert.False(vm.IsPasswordHidden);
            Assert.Equal(original, vm.DisplayedPassword);
        }

        [Fact]
        public void ViewModel_SetLengthPreset_SetsExpectedLength()
        {
            var vm = new MainViewModel();

            vm.SetLengthPresetCommand.Execute("32");
            Assert.Equal(32, vm.PasswordLength);
            Assert.Equal(32, vm.GeneratedPassword.Length);

            vm.SetLengthPresetCommand.Execute("12");
            Assert.Equal(12, vm.PasswordLength);
            Assert.Equal(12, vm.GeneratedPassword.Length);
        }

        [Fact]
        public void ViewModel_EnsureAtLeastOneSelected_PreventsEmptyCharacterPool()
        {
            var vm = new MainViewModel();

            // Odznaczamy po kolei
            vm.IncludeUppercase = false;
            vm.IncludeDigits = false;
            vm.IncludeSpecial = false;
            // Próba odznaczenia ostatniego (małe litery)
            vm.IncludeLowercase = false;

            // Powinno co najmniej jedno pozostać zaznaczone
            Assert.True(vm.IncludeUppercase || vm.IncludeLowercase || vm.IncludeDigits || vm.IncludeSpecial);
            Assert.False(string.IsNullOrEmpty(vm.GeneratedPassword));
        }

        [Fact]
        public void ViewModel_History_TracksGeneratedPasswords()
        {
            var vm = new MainViewModel();
            int initialCount = vm.History.Count;
            Assert.True(initialCount >= 1);

            vm.GenerateCommand.Execute(null);
            Assert.True(vm.History.Count >= 2);

            vm.ClearHistoryCommand.Execute(null);
            Assert.Empty(vm.History);
        }
    }
}
