using NUnit.Framework;
using Xamarin.UITest;

namespace ExampleFormsSolution.UITests
{
    public class MockDataTests : BaseTest
    {
        public MockDataTests(Platform platform) : base(platform)
        {
        }

        public override void BeforeEachTest()
        {
            base.BeforeEachTest();

            ItemsPage.WaitForPageToLoad();
        }

        [Test]
        public void AddNewItem()
        {
            //Arrange
            const string itemName = "Item Name";
            const string itemDescription = "Item Description";

            //Act
            ItemsPage.TapAddToolbarButton();

            NewItemPage.EnterItemName(itemName);
            NewItemPage.EnterItemDescription(itemDescription);
            NewItemPage.TapSaveToolbarButton();

            //Assert
            Assert.IsTrue(ItemsPage.IsPageVisible);
            Assert.IsTrue(app.Query(itemName).Length > 0);
        }

        [Ignore]
        [Test]
        public void Repl()
        {
            app.Repl();
        }
    }
}