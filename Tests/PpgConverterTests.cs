using Contracts.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class PpgConverterTests
    {
        private const string ACC_LINE  = "0.12,-0.34,9.81,1749720000000";
        private const string HR_LINE   = "72,1749720000000";
        private const string BVP_LINE  = "-3.14,1749720000000";
        private const string PARTICIPANT = "P01";

        // --- IBI parsing ---

        [TestMethod]
        public void IBI_FloatSeconds_ConvertedToMs()
        {
            // 0.828125 seconds = 828 ms
            var sample = PpgConverter.ConvertToOnePpgSample(ACC_LINE, HR_LINE, BVP_LINE, "0.828125", PARTICIPANT, 0);
            Assert.AreEqual(828, sample.IBI_ms);
        }

        [TestMethod]
        public void IBI_Null_ResultsInZero()
        {
            var sample = PpgConverter.ConvertToOnePpgSample(ACC_LINE, HR_LINE, BVP_LINE, null, PARTICIPANT, 0);
            Assert.AreEqual(0, sample.IBI_ms);
        }

        [TestMethod]
        public void IBI_LargeDuration_1Second_ConvertedToMs()
        {
            // 1.0 second = 1000 ms
            var sample = PpgConverter.ConvertToOnePpgSample(ACC_LINE, HR_LINE, BVP_LINE, "1.000000", PARTICIPANT, 0);
            Assert.AreEqual(1000, sample.IBI_ms);
        }

        // --- ACC parsing ---

        [TestMethod]
        public void ACC_Parsed_Correctly()
        {
            var sample = PpgConverter.ConvertToOnePpgSample(ACC_LINE, HR_LINE, BVP_LINE, null, PARTICIPANT, 0);
            Assert.AreEqual(0.12, sample.AccX.GetValueOrDefault(), 0.001);
            Assert.AreEqual(-0.34, sample.AccY.GetValueOrDefault(), 0.001);
            Assert.AreEqual(9.81, sample.AccZ.GetValueOrDefault(), 0.001);
        }

        // --- HR parsing ---

        [TestMethod]
        public void HR_Parsed_Correctly()
        {
            var sample = PpgConverter.ConvertToOnePpgSample(ACC_LINE, HR_LINE, BVP_LINE, null, PARTICIPANT, 0);
            Assert.AreEqual(72, sample.HeartRate);
        }

        // --- BVP parsing (can be negative) ---

        [TestMethod]
        public void BVP_Negative_ParsedCorrectly()
        {
            var sample = PpgConverter.ConvertToOnePpgSample(ACC_LINE, HR_LINE, BVP_LINE, null, PARTICIPANT, 0);
            Assert.AreEqual(-3.14, sample.PpgGreen.GetValueOrDefault(), 0.001);
            Assert.AreEqual(-3.14, sample.PpgRed.GetValueOrDefault(), 0.001);
            Assert.AreEqual(-3.14, sample.PpgIr.GetValueOrDefault(), 0.001);
        }

        // --- Participant / row index ---

        [TestMethod]
        public void ParticipantId_And_RowIndex_Set()
        {
            var sample = PpgConverter.ConvertToOnePpgSample(ACC_LINE, HR_LINE, BVP_LINE, null, PARTICIPANT, 7);
            Assert.AreEqual(PARTICIPANT, sample.ParticipantId);
            Assert.AreEqual(7, sample.RowIndex);
        }
    }
}
