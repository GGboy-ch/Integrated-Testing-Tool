using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace LogProfiler.ViewModels
{
    public class RangeObservableCollection<T> : ObservableCollection<T>
    {
        public void ReplaceRange(IEnumerable<T> items)
        {
            Items.Clear();
            foreach (T item in items)
            {
                Items.Add(item);
            }

            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
