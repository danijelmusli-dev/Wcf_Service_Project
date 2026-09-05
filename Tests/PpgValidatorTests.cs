using Contracts.Models;
using Contracts.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class PpgValidatorTests
    {
        // --- IBI ---

        [TestMethod]
        public void IBI_Zero_PassesValidation()
        {
            // IBI_ms=0 means no IBI data for this sample, not a bad value
            var sample = new PpgSample { IBI_ms = 0, HeartRate = 75 };
            Assert.IsTrue(PpgSampleValidator.ValidateSampleIBI(sample));
        }

        [TestMethod]
        public void IBI_ValidRange_Passes()
        {
            var sample = new PpgSample { IBI_ms = 828, HeartRate = 72 };
            Assert.IsTrue(PpgSampleValidator.ValidateSampleIBI(sample));
        }

        [TestMethod]
        public void IBI_TooLow_Fails()
        {
            var sample = new PpgSample { IBI_ms = 249, HeartRate = 72 };
            Assert.IsFalse(PpgSampleValidator.ValidateSampleIBI(sample));
        }

        [TestMethod]
        public void IBI_TooHigh_Fails()
        {
            var sample = new PpgSample { IBI_ms = 2001, HeartRate = 72 };
            Assert.IsFalse(PpgSampleValidator.ValidateSampleIBI(sample));
        }

        [TestMethod]
        public void IBI_BoundaryLow_Passes()
        {
            var sample = new PpgSample { IBI_ms = 250, HeartRate = 72 };
            Assert.IsTrue(PpgSampleValidator.ValidateSampleIBI(sample));
        }

        [TestMethod]
        public void IBI_BoundaryHigh_Passes()
        {
            var sample = new PpgSample { IBI_ms = 2000, HeartRate = 72 };
            Assert.IsTrue(PpgSampleValidator.ValidateSampleIBI(sample));
        }

        // --- PPG (E4 BVP signal can be negative) ---

        [TestMethod]
        public void Ppg_NegativeValue_PassesValidation()
        {
            var sample = new PpgSample { PpgGreen = -5.3, PpgRed = -5.3, PpgIr = -5.3 };
            Assert.IsTrue(PpgSampleValidator.ValidateSamplePpg(sample));
        }

        [TestMethod]
        public void Ppg_PositiveValue_Passes()
        {
            var sample = new PpgSample { PpgGreen = 12.7, PpgRed = 12.7, PpgIr = 12.7 };
            Assert.IsTrue(PpgSampleValidator.ValidateSamplePpg(sample));
        }

        [TestMethod]
        public void Ppg_Zero_Passes()
        {
            var sample = new PpgSample { PpgGreen = 0, PpgRed = 0, PpgIr = 0 };
            Assert.IsTrue(PpgSampleValidator.ValidateSamplePpg(sample));
        }

        // --- HR ---

        [TestMethod]
        public void HR_Valid_Passes()
        {
            var sample = new PpgSample { HeartRate = 75 };
            Assert.IsTrue(PpgSampleValidator.ValidateSampleHR(sample));
        }

        [TestMethod]
        public void HR_TooLow_Fails()
        {
            var sample = new PpgSample { HeartRate = 29 };
            Assert.IsFalse(PpgSampleValidator.ValidateSampleHR(sample));
        }

        [TestMethod]
        public void HR_TooHigh_Fails()
        {
            var sample = new PpgSample { HeartRate = 221 };
            Assert.IsFalse(PpgSampleValidator.ValidateSampleHR(sample));
        }

        [TestMethod]
        public void Null_Sample_Fails()
        {
            Assert.IsFalse(PpgSampleValidator.ValidateSampleHR(null));
            Assert.IsFalse(PpgSampleValidator.ValidateSampleIBI(null));
            Assert.IsFalse(PpgSampleValidator.ValidateSamplePpg(null));
        }
    }
}
