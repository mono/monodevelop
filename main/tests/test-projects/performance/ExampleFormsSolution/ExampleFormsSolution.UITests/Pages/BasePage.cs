using Xamarin.UITest;

namespace ExampleFormsSolution.UITests
{
    public abstract class BasePage
    {
        readonly string _pageTitle;

        protected readonly IApp app;
        protected readonly bool OnAndroid;
        protected readonly bool OniOS;

        protected BasePage(IApp app, Platform platform, string pageTitle)
        {
            this.app = app;

            OnAndroid = platform == Platform.Android;
            OniOS = platform == Platform.iOS;

            _pageTitle = pageTitle;

        }
        public bool IsPageVisible => app.Query(_pageTitle).Length > 0;

        public void WaitForPageToLoad()
        {
            app.WaitForElement(_pageTitle);
        }
    }
}