using FUT404DS;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FUT404DSTests
{
    [TestClass]
    public class GyroToggleStateTests
    {
        [TestMethod]
        public void ToggleChangesOnlyOnRisingEdge()
        {
            bool previous = false;
            bool toggled = false;

            Assert.IsTrue(Mouse.ApplyGyroToggleState(true, true,
                ref previous, ref toggled));
            Assert.IsTrue(Mouse.ApplyGyroToggleState(true, true,
                ref previous, ref toggled));
            Assert.IsTrue(Mouse.ApplyGyroToggleState(true, false,
                ref previous, ref toggled));
            Assert.IsFalse(Mouse.ApplyGyroToggleState(true, true,
                ref previous, ref toggled));
        }

        [TestMethod]
        public void HoldModeReturnsPhysicalTriggerState()
        {
            bool previous = false;
            bool toggled = true;

            Assert.IsTrue(Mouse.ApplyGyroToggleState(false, true,
                ref previous, ref toggled));
            Assert.IsFalse(Mouse.ApplyGyroToggleState(false, false,
                ref previous, ref toggled));
        }
    }
}
