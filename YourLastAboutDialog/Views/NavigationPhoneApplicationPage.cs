using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using YourLastAboutDialog.ViewModels;

namespace YourLastAboutDialog.Views
{
    /// <summary>
    /// A class derived from the <c>PhoneApplicationPage</c> class.
    /// Makes use of the <c>NavigationViewModelBase</c> view model base class
    /// to notify the view model about navigation events.
    /// </summary>
    public class NavigationPhoneApplicationPage : PhoneApplicationPage
    {
        protected NavigationViewModelBase NavigationViewModelBase
        {
            get
            {
                return DataContext as NavigationViewModelBase;
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);

            if (!e.Cancel)
            {
                var vm = NavigationViewModelBase;
                if (vm != null)
                {
                    vm.InternalNavigatingFrom(e);
                }
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            var vm = NavigationViewModelBase;
            if (vm != null)
            {
                vm.InternalNavigatedFrom(e);
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var vm = NavigationViewModelBase;
            if (vm != null)
            {
                vm.NavigationContext = NavigationContext;
                vm.InternalNavigatedTo(e);
            }
        }
    }
}
