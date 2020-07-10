using NUnit.Framework;
using System.Collections.Generic;
using SunServices.Helpers;
using System.IO;
using FakeItEasy;
using System.Linq;
using System.Buffers.Text;

namespace SunServices.Tests
{
    public class Base64HelperTest
    {
        [Test, Order(1)]
        public void Base64HelperTest_EncodeDecode()
        {
            string encode = Base64Helper.Encode("TestString");
            string decode = Base64Helper.Decode(encode);
            Assert.IsTrue(decode == "TestString");
        }



    }
}