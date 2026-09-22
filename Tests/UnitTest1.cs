using System;
using System.Linq;
using PasswordGenerator.Helpers;
using Xunit;

namespace PasswordGenerator.Tests
{
    public class PasswordGeneratorEngineTests
    {
        [Theory]
        [InlineData(4)]
        [InlineData(8)]
        [InlineData(16)]
        [InlineData(32)]
        [InlineData(64)]
        public void Generate_ReturnsPasswordWithExactRequestedLength(int length)
        {
            string password = PasswordGeneratorEngine.Generate(length, true, true, true, true);
            Assert.Equal(length, password.Length);
        }

        [Fact]
        public void Generate_WithAllCategories_ContainsAtLeastOneFromEachCategory()
        {
            for (int i = 0; i < 50; i++)
            {
                string password = PasswordGeneratorEngine.Generate(16, true, true, true, true);

                bool hasUpper = password.Any(char.IsUpper);
                bool hasLower = password.Any(char.IsLower);
                bool hasDigit = password.Any(char.IsDigit);
                bool hasSpecial = password.Any(c => "!@#$%^&*".Contains(c));

                Assert.True(hasUpper, "Password must contain uppercase");
                Assert.True(hasLower, "Password must contain lowercase");
                Assert.True(hasDigit, "Password must contain digit");
                Assert.True(hasSpecial, "Password must contain special char from !@#$%^&*");
            }
        }

        [Fact]
        public void Generate_WithSpecialCharsOnly_OnlyContainsAllowedSpecialChars()
        {
            for (int i = 0; i < 20; i++)
            {
                string password = PasswordGeneratorEngine.Generate(20, false, false, false, true);
                Assert.All(password, c => Assert.Contains(c, "!@#$%^&*"));
            }
        }

        [Fact]
        public void Generate_WithDigitsOnly_OnlyContainsDigits()
        {
            for (int i = 0; i < 20; i++)
            {
                string password = PasswordGeneratorEngine.Generate(15, false, false, true, false);
                Assert.All(password, c => Assert.True(char.IsDigit(c)));
            }
        }

        [Fact]
        public void Generate_WithExcludeAmbiguous_DoesNotContainAmbiguousCharacters()
        {
            for (int i = 0; i < 50; i++)
            {
                string password = PasswordGeneratorEngine.Generate(32, true, true, true, true, excludeAmbiguous: true);
                Assert.DoesNotContain('0', password);
                Assert.DoesNotContain('O', password);
                Assert.DoesNotContain('I', password);
                Assert.DoesNotContain('l', password);
                Assert.DoesNotContain('1', password);
            }
        }

        [Fact]
        public void Generate_WithNoCategories_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                PasswordGeneratorEngine.Generate(16, false, false, false, false));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void Generate_WithInvalidLength_ThrowsArgumentException(int length)
        {
            Assert.Throws<ArgumentException>(() =>
                PasswordGeneratorEngine.Generate(length, true, true, true, true));
        }

        [Fact]
        public void CalculateStrength_LongComplexPassword_ReturnsHighStrength()
        {
            string password = "P@ssw0rd!#2026Strong";
            int strength = PasswordGeneratorEngine.CalculateStrength(password);
            Assert.True(strength >= 75);
            string label = PasswordGeneratorEngine.GetStrengthLabel(strength);
            Assert.Contains(label, new[] { "Silne", "Bardzo silne" });
        }

        [Fact]
        public void CalculateStrength_ShortSimplePassword_ReturnsLowStrength()
        {
            string password = "abc";
            int strength = PasswordGeneratorEngine.CalculateStrength(password);
            Assert.True(strength <= 35);
            string label = PasswordGeneratorEngine.GetStrengthLabel(strength);
            Assert.Equal("Bardzo słabe", label);
        }

        [Fact]
        public void EstimateCrackTime_ReturnsMeaningfulString()
        {
            double entropy1 = 20;
            double entropy2 = 100;

            string estimate1 = PasswordGeneratorEngine.EstimateCrackTime(entropy1);
            string estimate2 = PasswordGeneratorEngine.EstimateCrackTime(entropy2);

            Assert.False(string.IsNullOrWhiteSpace(estimate1));
            Assert.False(string.IsNullOrWhiteSpace(estimate2));
            Assert.Equal("Miliony lat", estimate2);
        }
    }
}
