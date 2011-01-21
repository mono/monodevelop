using System;
using System.Linq;
using System.Reflection;
using Microsoft.WindowsAPICodePack.Shell;
using Xunit;

namespace Tests
{
    public class StockIconsTests
    {
        [Fact]
        public void StockIconsNotNull()
        {
            PropertyInfo[] infos = typeof(StockIcons).GetType().GetProperties(BindingFlags.Public);

            // BUG: This (StockIcons) doesn't follow the same pattern as most of the other factory style patterned classes?
            StockIcons icons = new StockIcons();
            
            foreach (PropertyInfo info in infos)
            {
                if (info.PropertyType == typeof(StockIcon))
                {                    
                    Assert.DoesNotThrow(new Assert.ThrowsDelegate(() =>
                    {
                        StockIcon test = (StockIcon)info.GetValue(icons, null);
                    }));
                }
            }
        }

        [Fact] 
        public void StockIconsContainedInAllCollection()
        {
            // TODO: Redesign test, this is a pretty redundant test with the way the class is designed.

            StockIcons icons = new StockIcons();

            PropertyInfo[] infos = typeof(StockIcons).GetType().GetProperties(BindingFlags.Public);
            foreach (var info in infos)
            {
                if (info.PropertyType != typeof(StockIcon)) continue;

                StockIcon icon = null;
                try
                {
                    //this will throw an exception if the icon does not exist on the system
                    icon = (StockIcon)info.GetValue(icons, null);
                }
                catch { continue; }
                
                Assert.Contains<StockIcon>(icon, icons.AllStockIcons);
            }
        }

        [Fact]
        public void VerifyAllStockIconsArePropertized()
        {
            PropertyInfo[] infos = typeof(StockIcons).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            int count = infos.Count(x => x.PropertyType == typeof(StockIcon));

            Assert.Equal(count, Enum.GetValues(typeof(StockIconIdentifier)).Length);
        }
    }
}
