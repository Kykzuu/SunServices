using NUnit.Framework;
using System.Collections.Generic;
using SunServices.Helpers;
using System.IO;
using FakeItEasy;
using System.Linq;

namespace SunServices.Tests
{
    public class FileDataHelperTest
    {

        public class Obj
        {
            public uint id { get; set; }
            public string nickname { get; set; }
        }

        [Test, Order(1)]
        public void FileDataHelperTest_WriteObject()
        {
            var obj = A.Fake<Obj>();
            FileDataHelper.Write(obj, "TestObject");

            FileAssert.Exists("Data/TestObject");
        }

        [Test, Order(1)]
        public void FileDataHelperTest_WriteObjects()
        {
            var obj = new List<Obj>();
            obj.Add(A.Fake<Obj>());
            obj.Add(A.Fake<Obj>());
            obj.Add(A.Fake<Obj>());

            FileDataHelper.Write(obj, "TestObjects");

            FileAssert.Exists("Data/TestObjects");
        }

        [Test, Order(2)]
        public void FileDataHelperTest_ReadObject()
        {
            var obj = FileDataHelper.Read<Obj>("TestObject");

            Assert.IsNotNull(obj);
        }

        [Test, Order(2)]
        public void FileDataHelperTest_ReadObjects()
        {
            var obj = FileDataHelper.Read<List<Obj>>("TestObjects");

            Assert.IsNotNull(obj);
        }


    }
}