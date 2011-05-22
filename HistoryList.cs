using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FileManager
{
    public class HistoryList : ObservableCollection<String>
    {
        public int MaxCount;
        public HistoryList(int count)
            : base()
        {
            MaxCount = count;
        }

        public new void Add(string item)
        {
            if (base.Contains(item))
            {
                base.Remove(item);
            }

            if (this.Count == MaxCount)
            {
                base.RemoveAt(0);
            }
            base.Add(item);
        }
    }
}
