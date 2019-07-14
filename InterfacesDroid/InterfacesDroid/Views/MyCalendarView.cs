using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Support.V4.View;
using Android.Util;
using Android.Database;
using Java.Lang;
using ToolsPortable;

namespace InterfacesDroid.Views
{
    public class MyCalendarView : ViewPager
    {
        public event EventHandler DisplayMonthChanged;
        public event EventHandler SelectedDateChanged;

        public MyCalendarView(Context context) : base(context)
        {
            Initialize();
        }

        public MyCalendarView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            Initialize();
        }

        private void Initialize()
        {
            base.PageSelected += MyCalendarView_PageSelected;
        }

        private void MyCalendarView_PageSelected(object sender, PageSelectedEventArgs e)
        {
            if (Adapter != null)
            {
                DisplayMonthChanged?.Invoke(this, new EventArgs());
            }
        }

        /// <summary>
        /// You can set the DisplayMonth by assigning a new Adapter
        /// </summary>
        public DateTime DisplayMonth
        {
            get
            {
                if (Adapter == null)
                {
                    return DateTime.MinValue;
                }

                return Adapter.GetMonth(this.CurrentItem);
            }
        }

        private DateTime? _selectedDate;
        public DateTime? SelectedDate
        {
            get
            {
                return _selectedDate;
            }

            set
            {
                if ((_selectedDate == null && value == null)
                    || (_selectedDate != null && value != null && value.Value.Date == _selectedDate.Value.Date))
                {
                    return;
                }

                if (value != null)
                {
                    value = value.Value.Date;
                }
                _selectedDate = value;
                SelectedDateChanged?.Invoke(this, new EventArgs());
            }
        }

        private CalendarAdapter _adapter;
        public new CalendarAdapter Adapter
        {
            get { return _adapter; }
            set
            {
                _adapter = value;

                if (value != null)
                {
                    base.Adapter = new MyCalendarPagerAdapter(value, this);
                    this.SetCurrentItem(1000, false);
                }

                else
                {
                    base.Adapter = null;
                }
            }
        }

        public abstract class CalendarAdapter
        {
            public DateTime CenterMonth { get; private set; }
            public DateTime FirstMonth { get; private set; }

            public CalendarAdapter(DateTime month)
            {
                month = DateTools.GetMonth(month);

                CenterMonth = month;
                FirstMonth = DateTools.GetMonth(month.AddMonths(-1000));
            }

            public int Count
            {
                get { return 2001; }
            }

            public abstract MyCalendarMonthView GetView(ViewGroup parent, MyCalendarView calendarView);

            public DateTime GetMonth(int position)
            {
                return DateTools.GetMonth(FirstMonth.AddMonths(position));
            }

            public int GetPosition(DateTime month)
            {
                return DateTools.DifferenceInMonths(month, FirstMonth);
            }
        }

        private class MyCalendarPagerAdapter : PagerAdapter
        {
            private CalendarAdapter _calendarAdapter;
            private MyCalendarView _calendarView;

            public MyCalendarPagerAdapter(CalendarAdapter calendarAdapter, MyCalendarView calendarView)
            {
                _calendarView = calendarView;
                _calendarAdapter = calendarAdapter;
            }

            public override int Count
            {
                get
                {
                    return _calendarAdapter.Count;
                }
            }
            
            public override bool IsViewFromObject(View view, Java.Lang.Object objectValue)
            {
                return view == objectValue;
            }

            public override Java.Lang.Object InstantiateItem(ViewGroup container, int position)
            {
                DateTime month = _calendarAdapter.GetMonth(position);

                MyCalendarMonthView monthView;
                if (_destroyedViews.Count > 0)
                {
                    monthView = _destroyedViews.Last();
                    _destroyedViews.RemoveAt(_destroyedViews.Count - 1);
                }
                else
                {
                    monthView = _calendarAdapter.GetView(container, _calendarView);
                    container.AddView(monthView);
                }

                monthView.Month = month;

                return monthView;
            }

            private List<MyCalendarMonthView> _destroyedViews = new List<MyCalendarMonthView>();

            public override void DestroyItem(ViewGroup container, int position, Java.Lang.Object objectValue)
            {
                var monthView = (MyCalendarMonthView)objectValue;
                _destroyedViews.Add(monthView);
            }
        }
    }
}