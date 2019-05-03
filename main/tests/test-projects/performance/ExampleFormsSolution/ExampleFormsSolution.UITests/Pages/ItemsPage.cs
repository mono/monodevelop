using Xamarin.UITest;

using Query = System.Func<Xamarin.UITest.Queries.AppQuery, Xamarin.UITest.Queries.AppQuery>;

namespace ExampleFormsSolution.UITests
{
    public class ItemsPage : BasePage
    {
        readonly Query _addToolbarButton;

        public ItemsPage(IApp app, Platform platform) : base(app, platform, "Browse")
        {
            if (OniOS)
                _addToolbarButton = x => x.Class("UIButtonLabel").Index(0);
            else
                _addToolbarButton = x => x.Class("android.support.v7.view.menu.ActionMenuItemView").Index(0);
        }

        public void TapAddToolbarButton()
        {
            app.Tap(_addToolbarButton);

            app.Screenshot("Toolbar Item Tapped");
        }
    }
}