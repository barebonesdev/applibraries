using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Foundation;
using UIKit;
using System.Collections;
using ToolsPortable;
using CoreGraphics;
using CoreAnimation;

namespace InterfacesiOS.Views.Calendar
{
    public abstract class BareUICalendarItemsSourceProvider
    {
        public abstract IEnumerable GetItemsSource(DateTime date);

        public virtual CGColor GetBackgroundColorForDate(DateTime date)
        {
            return null;
        }
    }

    public class BareUICalendarMonthView : UIView
    {
        public enum DayType
        {
            ThisMonth,
            PrevMonth,
            NextMonth
        }

        public event EventHandler<DateTime> DateClicked;

        private UILabel _labelMonth;
        private IBareUICalendarDayView[,] _dayViews = new IBareUICalendarDayView[6, 7];
        private BaseBareUIViewItemsSourceAdapter[,] _itemsSourceAdapters = new BaseBareUIViewItemsSourceAdapter[6, 7];

        public virtual float TopPadding => 16;
        public virtual float SpacingAfterTitle => 16;
        public virtual float SpacingAfterDayHeaders => 4;

        private UIView _viewTitle;
        private UIView _viewDayHeaders;
        private UIView[] _viewRows;

        public readonly DayOfWeek FirstDayOfWeek;

        ~BareUICalendarMonthView()
        {
            System.Diagnostics.Debug.WriteLine("Month view disposed");
        }

        /// <summary>
        /// Assign the Month property to initialize the view
        /// </summary>
        public BareUICalendarMonthView(DayOfWeek firstDayOfWeek)
        {
            FirstDayOfWeek = firstDayOfWeek;

            var title = CreateTitle();
            this.Add(title);
            _viewTitle = title;

            var dayHeaders = CreateDayHeaders();
            this.Add(dayHeaders);
            _viewDayHeaders = dayHeaders;

            _viewRows = new UIView[6];

            for (int i = 0; i < 6; i++)
            {
                var row = CreateRow(out List<IBareUICalendarDayView> createdDays);
                for (int x = 0; x < 7; x++)
                {
                    var dayView = createdDays[x];
                    _dayViews[i, x] = dayView;
                    // Can't store these adapters on the day view items themselves, since that gives the items
                    // a strong reference to the Month view via the method for creating the item views
                    _itemsSourceAdapters[i, x] = CreateItemsSourceAdapter(dayView.ItemsView);

                    if (dayView is UIControl)
                    {
                        (dayView as UIControl).TouchUpInside += new WeakEventHandler(DayView_TouchUpInside).Handler;
                    }
                }

                this.Add(row);
                _viewRows[i] = row;
            }
        }

        public void UpdateAllBackgroundColors()
        {
            if (Provider == null)
            {
                return;
            }

            foreach (var dayView in _dayViews)
            {
                dayView.SetBackgroundColor(Provider.GetBackgroundColorForDate(dayView.Date));
            }
        }

        private void DayView_TouchUpInside(object sender, EventArgs e)
        {
            IBareUICalendarDayView dayView = (IBareUICalendarDayView)sender;
            DateClicked?.Invoke(this, dayView.Date);
        }

        public override void LayoutSubviews()
        {
            var titleSize = _viewTitle.SizeThatFits(this.Frame.Size);
            var dayHeadersSize = _viewDayHeaders.SizeThatFits(new CGSize(this.Frame.Width, this.Frame.Height));

            nfloat y = TopPadding;

            _viewTitle.Frame = new CGRect(
                x: 16,
                y: y,
                width: this.Frame.Width,
                height: titleSize.Height);

            y += titleSize.Height + SpacingAfterTitle;

            _viewDayHeaders.Frame = new CGRect(
                x: 0,
                y: y,
                width: this.Frame.Width,
                height: dayHeadersSize.Height);

            y += dayHeadersSize.Height + SpacingAfterDayHeaders;

            nfloat remainingHeight = this.Frame.Height - y;
            if (remainingHeight > 0)
            {
                nfloat rowHeight = remainingHeight / 6;

                foreach (var row in _viewRows)
                {
                    row.Frame = new CGRect(
                        x: 0,
                        y: y,
                        width: this.Frame.Width,
                        height: rowHeight);

                    y += rowHeight;
                }
            }
        }

        private BareUICalendarItemsSourceProvider _provider;
        public BareUICalendarItemsSourceProvider Provider
        {
            get { return _provider; }
            set
            {
                _provider = value;

                RefreshData();
            }
        }

        private DateTime _month;
        public DateTime Month
        {
            get { return _month; }
            set
            {
                value = DateTools.GetMonth(value);

                if (_month == value)
                {
                    return;
                }

                _month = value;

                OnMonthChanged();
            }
        }

        private IBareUICalendarDayView _currSelectedDayView;
        private DateTime _selectedDate;
        public DateTime SelectedDate
        {
            get { return _selectedDate; }
            set
            {
                if (_selectedDate == value.Date)
                {
                    return;
                }

                var prevDate = _selectedDate;
                _selectedDate = value.Date;

                if (_currSelectedDayView != null)
                {
                    UpdateDay(_currSelectedDayView, prevDate, false);
                    _currSelectedDayView = null;
                }

                OnSelectedDateChanged();
            }
        }

        private void RefreshData()
        {
            if (Provider == null || Month == DateTime.MinValue)
            {
                return;
            }

            for (int i = 0; i < 6; i++)
            {
                for (int x = 0; x < 7; x++)
                {
                    var adapter = _itemsSourceAdapters[i, x];
                    if (adapter != null)
                    {
                        var date = _dayViews[i, x].Date;
                        adapter.ItemsSource = Provider.GetItemsSource(date);
                    }
                }
            }

            UpdateAllBackgroundColors();
        }

        protected virtual void OnMonthChanged()
        {
            if (_labelMonth != null)
                _labelMonth.Text = Month.ToString("MMMM yyyy");

            DateTime[,] array = CalendarArray.Generate(Month, FirstDayOfWeek);

            for (int row = 0; row < 6; row++)
            {
                for (int col = 0; col < 7; col++)
                {
                    DateTime date = array[row, col];
                    IBareUICalendarDayView dayView = _dayViews[row, col];

                    UpdateDay(dayView, date, isSelected: date == SelectedDate);
                }
            }

            RefreshData();
        }

        private void UpdateDay(IBareUICalendarDayView dayView, DateTime date, bool isSelected)
        {
            DayType dayType;

            if (DateTools.SameMonth(date, Month))
            {
                dayType = DayType.ThisMonth;
            }
            else if (date < Month)
            {
                dayType = DayType.PrevMonth;
            }
            else
            {
                dayType = DayType.NextMonth;
            }

            dayView.UpdateDay(date, dayType, date.Date == DateTime.Today, isSelected);

            if (isSelected)
            {
                _currSelectedDayView = dayView;
            }
        }

        protected virtual void OnSelectedDateChanged()
        {
            DateTime[,] array = CalendarArray.Generate(Month, FirstDayOfWeek);

            for (int row = 0; row < 6; row++)
            {
                for (int col = 0; col < 7; col++)
                {
                    DateTime date = array[row, col];

                    if (date == SelectedDate)
                    {
                        IBareUICalendarDayView dayView = _dayViews[row, col];

                        UpdateDay(dayView, date, isSelected: true);
                        return;
                    }
                }
            }
        }

        protected virtual UIView CreateTitle()
        {
            _labelMonth = new UILabel()
            {
                Font = UIFont.PreferredTitle3
            };
            return _labelMonth;
        }

        protected virtual UIView CreateDayHeaders()
        {
            return new DayHeadersView(FirstDayOfWeek);
        }

        protected class DayHeadersView : UIView
        {
            private nfloat heightOfText;

            public DayHeadersView(DayOfWeek firstDayOfWeek)
            {
                DayOfWeek day = firstDayOfWeek;
                for (int i = 0; i < 7; i++, day++)
                {
                    var label = new UILabel()
                    {
                        Text = DateTools.ToLocalizedString(day).Substring(0, 1).ToUpper(),
                        Font = UIFont.PreferredCaption1,
                        TextAlignment = UITextAlignment.Center
                    };

                    if (day == DayOfWeek.Saturday || day == DayOfWeek.Sunday)
                    {
                        label.TextColor = UIColor.Gray;
                    }

                    this.Add(label);
                }

                heightOfText = this.Subviews.First().SizeThatFits(new CGSize(nfloat.MaxValue, nfloat.MaxValue)).Height;
            }

            public override CGSize SizeThatFits(CGSize size)
            {
                // We're always going to ignore the width anyways
                return new CGSize(0, heightOfText);
            }

            public override void LayoutSubviews()
            {
                int total = this.Subviews.Length;
                nfloat each = this.Frame.Width / total;
                for (int i = 0; i < total; i++)
                {
                    this.Subviews[i].Frame = new CGRect(
                        x: each * i,
                        y: 0,
                        width: each,
                        height: heightOfText);
                }
            }
        }

        protected virtual UIView CreateRow(out List<IBareUICalendarDayView> createdDays)
        {
            return new RowView(CreateDay, out createdDays);
        }

        private class RowView : UIView
        {
            private static readonly nfloat SEPARATOR_HEIGHT = 0.5f;
            private UIView _separator;
            private UIView[] _days;

            public RowView(Func<UIView> createDay, out List<IBareUICalendarDayView> createdDays)
            {
                _separator = new UIView()
                {
                    BackgroundColor = UIColor.LightGray
                };
                this.Add(_separator);

                _days = new UIView[7];
                createdDays = new List<IBareUICalendarDayView>();

                for (int i = 0; i < 7; i++)
                {
                    UIView dayView = createDay();

                    if (!(dayView is IBareUICalendarDayView))
                    {
                        throw new InvalidOperationException("CreateDay must return a UIView that implements IBareUICalendarDayView");
                    }
                    createdDays.Add(dayView as IBareUICalendarDayView);
                    _days[i] = dayView;

                    this.Add(dayView);
                }
            }

            public override void LayoutSubviews()
            {
                _separator.Frame = new CGRect(
                    x: 0,
                    y: 0,
                    width: this.Frame.Width,
                    height: SEPARATOR_HEIGHT);

                nfloat colWidth = this.Frame.Width / _days.Length;
                for (int i = 0; i < _days.Length; i++)
                {
                    _days[i].Frame = new CGRect(
                        x: colWidth * i,
                        y: 0,
                        width: colWidth,
                        height: this.Frame.Height);
                }
            }
        }

        protected virtual UIControl CreateDay()
        {
            return new BareUICalendarDayView();
        }

        /// <summary>
        /// The items source adapter for the individal day items
        /// </summary>
        /// <param name="stackView"></param>
        /// <returns></returns>
        protected virtual BaseBareUIViewItemsSourceAdapter CreateItemsSourceAdapter(UIView itemsView)
        {
            return new BareUIViewItemsSourceAdapter(itemsView, DefaultItemView);
        }

        private UIView DefaultItemView(object item)
        {
            return new BareUIEllipseView()
            {
                FillColor = GetColorForItem(item),
                UserInteractionEnabled = false
            };
        }

        protected virtual CGColor GetColorForItem(object item)
        {
            return UIColor.Gray.CGColor;
        }
    }

    public interface IBareUICalendarDayView
    {
        DateTime Date { get; }
        UIView ItemsView { get; }

        void UpdateDay(DateTime date, BareUICalendarMonthView.DayType dayType, bool isToday, bool isSelected);

        void SetBackgroundColor(CGColor color);
    }

    public class BareUICalendarDayView : UIControl, IBareUICalendarDayView
    {
        private BareUIEllipseView _backgroundCircle;
        private UILabel _labelDay;
        private UIView _itemsView;

        public BareUICalendarDayView()
        {
            _backgroundCircle = new BareUIEllipseView()
            {
                AspectRatio = BareUIEllipseView.AspectRatios.Circle,
                UserInteractionEnabled = false
            };
            this.Add(_backgroundCircle);

            _labelDay = new UILabel()
            {
                Font = UIFont.PreferredBody,
                TextAlignment = UITextAlignment.Center
            };
            this.Add(_labelDay);

            _itemsView = new BareUICalendarCircleItemsView();
            this.Add(_itemsView);
        }

        private static readonly nfloat TOP_SPACING = 4;
        private static readonly nfloat CIRCLE_HEIGHT = 35;
        private static readonly nfloat SPACING_AFTER_CIRCLE = 4;

        public override void LayoutSubviews()
        {
            _backgroundCircle.Frame = new CGRect(
                x: 0,
                y: TOP_SPACING,
                width: this.Frame.Width,
                height: CIRCLE_HEIGHT);

            _labelDay.Frame = new CGRect(
                x: 0,
                y: TOP_SPACING,
                width: this.Frame.Width,
                height: CIRCLE_HEIGHT);

            nfloat y = TOP_SPACING + CIRCLE_HEIGHT + SPACING_AFTER_CIRCLE;
            nfloat remainingHeight = this.Frame.Height - y;
            remainingHeight = remainingHeight >= 0 ? remainingHeight : 0;

            _itemsView.Frame = new CGRect(
                x: 0,
                y: y,
                width: this.Frame.Width,
                height: remainingHeight);

            if (_backgroundLayer != null)
            {
                _backgroundLayer.Path = CGPath.FromRect(new CGRect(
                    x: 0,
                    y: 0,
                    width: this.Frame.Width,
                    height: this.Frame.Height));
            }
        }

        public DateTime Date { get; private set; }

        public UIView ItemsView => _itemsView;

        public void UpdateDay(DateTime date, BareUICalendarMonthView.DayType dayType, bool isToday, bool isSelected)
        {
            Date = date.Date;

            _labelDay.Text = date.Day.ToString();

            if (dayType == BareUICalendarMonthView.DayType.NextMonth || dayType == BareUICalendarMonthView.DayType.PrevMonth)
            {
                _labelDay.TextColor = UIColor.LightGray;
            }
            else if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            {
                _labelDay.TextColor = UIColor.Gray;
            }
            else
            {
                _labelDay.TextColor = UIColor.Black;
            }

            if (isSelected)
            {
                _backgroundCircle.FillColor = this.TintColor.CGColor;
                _labelDay.TextColor = UIColor.White;
            }
            else if (isToday)
            {
                _backgroundCircle.FillColor = UIColor.DarkGray.CGColor;
                _labelDay.TextColor = UIColor.White;
            }
            else
            {
                _backgroundCircle.FillColor = UIColor.Clear.CGColor;
            }
        }

        private CAShapeLayer _backgroundLayer;
        public void SetBackgroundColor(CGColor color)
        {
            if (color == null)
            {
                if (_backgroundLayer != null)
                {
                    _backgroundLayer.RemoveFromSuperLayer();
                    _backgroundLayer = null;
                }
                return;
            }

            if (_backgroundLayer == null)
            {
                _backgroundLayer = new CAShapeLayer();
                base.Layer.AddSublayer(_backgroundLayer);
            }
            _backgroundLayer.FillColor = color;
        }
    }

    public class BareUICalendarCircleItemsView : UIView
    {
        public static readonly nfloat CIRCLE_SIZE = 8;
        public static readonly nfloat SPACING = 4;

        public BareUICalendarCircleItemsView()
        {
            UserInteractionEnabled = false;
        }

        public override CGSize SizeThatFits(CGSize size)
        {
            int count = Subviews.Length;
            if (count == 0 || size.Width == 0)
            {
                return new CGSize();
            }

            nfloat totalSpacing = (count - 1) * SPACING;
            nfloat totalWidth = count * CIRCLE_SIZE + totalSpacing;
            nfloat itemSize = CIRCLE_SIZE;

            if (totalWidth > size.Width)
            {
                if (totalSpacing >= size.Width)
                {
                    itemSize = 1;
                }
                else
                {
                    itemSize = (size.Width - totalSpacing) / count;
                    if (itemSize < 1)
                    {
                        itemSize = 1;
                    }
                }

                return new CGSize(size.Width, itemSize);
            }
            else
            {
                return new CGSize(totalWidth, itemSize);
            }
        }

        public override void LayoutSubviews()
        {
            var finalSize = SizeThatFits(this.Frame.Size);
            if (finalSize.Width == 0)
            {
                return;
            }

            nfloat itemSize = finalSize.Height;

            nfloat x = 0;
            nfloat y = 0;

            if (itemSize < CIRCLE_SIZE)
            {
                y = (CIRCLE_SIZE - itemSize) / 2;
            }

            if (finalSize.Width < this.Frame.Width)
            {
                x = (this.Frame.Width - finalSize.Width) / 2;
            }

            foreach (var subview in Subviews)
            {
                subview.Frame = new CGRect(
                    x: x,
                    y: y,
                    width: itemSize,
                    height: itemSize);

                x += itemSize + SPACING;
            }
        }
    }
}