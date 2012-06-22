﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AxeSoftware.Quest;

namespace WorldModelTests
{
    [TestClass]
    public class FieldExtensionTests
    {
        [TestMethod]
        public void TestListExtension_Simple()
        {
            WorldModel worldModel = new WorldModel();

            Element defaultType = worldModel.GetElementFactory(ElementType.ObjectType).Create("defaultobject");
            defaultType.Fields.Set("listfield", new QuestList<string>());

            Element type1 = worldModel.GetElementFactory(ElementType.ObjectType).Create("type1");
            type1.Fields.AddFieldExtension("listfield", new QuestList<string>(new[] { "a" }, true));

            Element type2 = worldModel.GetElementFactory(ElementType.ObjectType).Create("type2");
            type2.Fields.AddFieldExtension("listfield", new QuestList<string>(new[] { "b" }, true));

            Element obj = worldModel.GetElementFactory(ElementType.Object).Create("object");
            obj.Fields.AddType(type1);
            obj.Fields.AddType(type2);
            obj.Fields.Resolve(null);

            var result = obj.Fields.GetAsType<QuestList<string>>("listfield");

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("a"));
            Assert.IsTrue(result.Contains("b"));
        }
    }
}
