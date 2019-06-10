using Xamarin.UITest;

using Query = System.Func<Xamarin.UITest.Queries.AppQuery, Xamarin.UITest.Queries.AppQuery>;

namespace ExampleFormsSolution.UITests
{
    public class NewItemPage : BasePage
    {
        readonly Query _itemNameEntry, _itemDescriptionEditor, _saveToolbarItem;

        public NewItemPage(IApp app, Platform platform) : base(app, platform, "New Item")
        {
            if (OniOS)
            {
                _itemNameEntry = x => x.Class("UITextField").Index(0);
                _itemDescriptionEditor = x => x.Class("UITextView").Index(0);
                _saveToolbarItem = x => x.Class("UIButtonLabel").Index(0);
            }
            else
            {
                _itemNameEntry = x => x.Class("FormsEditText").Index(0);
                _itemDescriptionEditor = x => x.Class("FormsEditText").Index(1);
                _saveToolbarItem = x => x.Class("android.support.v7.view.menu.ActionMenuItemView").Index(0);
            }
        }

        public void EnterItemName(string text)
        {
            EnterText(_itemNameEntry, text);

            app.Screenshot("Entered Item Name");
        }

        public void EnterItemDescription(string text)
        {
            EnterText(_itemDescriptionEditor, text);

            app.Screenshot("Entered Item Description");
        }

        public void TapSaveToolbarButton()
        {
            app.Tap(_saveToolbarItem);

            app.Screenshot("Save Toolbar Item Tapped");
        }

        void EnterText(Query textBoxQuery, string text)
        {
            app.ClearText(textBoxQuery);
            app.EnterText(textBoxQuery, text);
            app.DismissKeyboard();
        }
    }
}