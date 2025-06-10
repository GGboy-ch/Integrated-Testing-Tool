using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DeviceEmulator.ViewModel
{
    public class NotifyBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        /// <summary>
        /// 自定义通知方法
        /// </summary>
        /// <param name="propertyName"></param>
        protected void MyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
