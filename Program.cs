using System;
using System.Collections.Generic;
using Gtk;

namespace SimpleCalendarGtk
{
    class CalendarEvent
    {
        public string Title;
        public string Description;
        public DateTime Date;

        public CalendarEvent(string title, string description, DateTime date)
        {
            Title = title;
            Description = description;
            Date = date;
        }
    }

    class CalendarApp : Window
    {
        DateTime currentMonth = DateTime.Today;
        DateTime selectedDate = DateTime.Today;

        Label monthLabel;
        Label selectedDateLabel;
        Grid calendarGrid;
        ListBox eventList;

        Entry titleEntry;
        Entry descriptionEntry;
        SpinButton hourSpin;
        SpinButton minuteSpin;

        List<CalendarEvent> eventsList = new List<CalendarEvent>();

        public CalendarApp() : base("Mini Calendar")
        {
            SetDefaultSize(850, 600);
            SetPosition(WindowPosition.Center);
            DeleteEvent += delegate { Application.Quit(); };

            VBox mainBox = new VBox(false, 8);
            mainBox.BorderWidth = 10;

            mainBox.PackStart(CreateHeader(), false, false, 0);

            HBox content = new HBox(false, 10);
            content.PackStart(CreateCalendarPart(), true, true, 0);
            content.PackStart(CreateEventPart(), false, false, 0);

            mainBox.PackStart(content, true, true, 0);
            Add(mainBox);

            DrawCalendar();
            RefreshEvents();
            ShowAll();
        }

        HBox CreateHeader()
        {
            HBox box = new HBox(false, 10);

            Button prev = new Button("<");
            Button next = new Button(">");
            Button today = new Button("Today");

            monthLabel = new Label();
            monthLabel.SetSizeRequest(250, 30);

            prev.Clicked += delegate
            {
                currentMonth = currentMonth.AddMonths(-1);
                DrawCalendar();
            };

            next.Clicked += delegate
            {
                currentMonth = currentMonth.AddMonths(1);
                DrawCalendar();
            };

            today.Clicked += delegate
            {
                currentMonth = DateTime.Today;
                selectedDate = DateTime.Today;
                DrawCalendar();
                RefreshEvents();
            };

            box.PackStart(prev, false, false, 0);
            box.PackStart(today, false, false, 0);
            box.PackStart(monthLabel, true, true, 0);
            box.PackStart(next, false, false, 0);

            return box;
        }

        VBox CreateCalendarPart()
        {
            VBox box = new VBox(false, 8);

            Label title = new Label();
            title.Markup = "<b>Calendar</b>";

            calendarGrid = new Grid();
            calendarGrid.RowSpacing = 4;
            calendarGrid.ColumnSpacing = 4;

            box.PackStart(title, false, false, 0);
            box.PackStart(calendarGrid, true, true, 0);

            return box;
        }

        VBox CreateEventPart()
        {
            VBox box = new VBox(false, 8);
            box.SetSizeRequest(280, -1);

            selectedDateLabel = new Label();

            Label listTitle = new Label();
            listTitle.Markup = "<b>Events</b>";

            eventList = new ListBox();

            ScrolledWindow scroll = new ScrolledWindow();
            scroll.SetSizeRequest(260, 220);
            scroll.SetPolicy(PolicyType.Automatic, PolicyType.Automatic);
            scroll.Add(eventList);

            titleEntry = new Entry();
            titleEntry.PlaceholderText = "Event title";

            descriptionEntry = new Entry();
            descriptionEntry.PlaceholderText = "Description";

            hourSpin = new SpinButton(0, 23, 1);
            minuteSpin = new SpinButton(0, 59, 1);

            HBox timeBox = new HBox(false, 5);
            timeBox.PackStart(new Label("Hour"), false, false, 0);
            timeBox.PackStart(hourSpin, false, false, 0);
            timeBox.PackStart(new Label("Min"), false, false, 0);
            timeBox.PackStart(minuteSpin, false, false, 0);

            Button addButton = new Button("Add Event");
            Button deleteButton = new Button("Delete Selected");

            addButton.Clicked += AddEvent;
            deleteButton.Clicked += DeleteEventFromList;

            box.PackStart(selectedDateLabel, false, false, 0);
            box.PackStart(listTitle, false, false, 0);
            box.PackStart(scroll, true, true, 0);
            box.PackStart(new Label("Add new event"), false, false, 0);
            box.PackStart(titleEntry, false, false, 0);
            box.PackStart(descriptionEntry, false, false, 0);
            box.PackStart(timeBox, false, false, 0);
            box.PackStart(addButton, false, false, 0);
            box.PackStart(deleteButton, false, false, 0);

            return box;
        }

        void DrawCalendar()
        {
            foreach (Widget child in calendarGrid.Children)
                calendarGrid.Remove(child);

            monthLabel.Markup = $"<b>{currentMonth:MMMM yyyy}</b>";

            string[] days = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };

            for (int i = 0; i < days.Length; i++)
            {
                Label label = new Label();
                label.Markup = $"<b>{days[i]}</b>";
                calendarGrid.Attach(label, i, 0, 1, 1);
            }

            DateTime first = new DateTime(currentMonth.Year, currentMonth.Month, 1);
            int startColumn = GetColumn(first);
            int daysInMonth = DateTime.DaysInMonth(currentMonth.Year, currentMonth.Month);

            int row = 1;
            int column = startColumn;

            for (int day = 1; day <= daysInMonth; day++)
            {
                DateTime date = new DateTime(currentMonth.Year, currentMonth.Month, day);
                Button button = MakeDayButton(date);

                calendarGrid.Attach(button, column, row, 1, 1);

                column++;
                if (column == 7)
                {
                    column = 0;
                    row++;
                }
            }

            UpdateSelectedDate();
            calendarGrid.ShowAll();
        }

        Button MakeDayButton(DateTime date)
        {
            VBox box = new VBox(false, 2);

            Label number = new Label(date.Day.ToString());
            Label info = new Label();

            int count = CountEvents(date);
            if (count > 0) info.Text = count + " event(s)";
            else info.Text = "";

            box.PackStart(number, false, false, 0);
            box.PackStart(info, false, false, 0);

            Button button = new Button();
            button.SetSizeRequest(85, 65);
            button.Add(box);

            if (date.Date == DateTime.Today.Date)
                button.TooltipText = "Today";

            button.Clicked += delegate
            {
                selectedDate = date;
                DrawCalendar();
                RefreshEvents();
            };

            return button;
        }

        int GetColumn(DateTime date)
        {
            int day = (int)date.DayOfWeek;
            if (day == 0) return 6;
            return day - 1;
        }

        void RefreshEvents()
        {
            foreach (Widget child in eventList.Children)
                eventList.Remove(child);

            List<CalendarEvent> todayEvents = GetEvents(selectedDate);
            todayEvents.Sort((a, b) => a.Date.CompareTo(b.Date));

            if (todayEvents.Count == 0)
            {
                ListBoxRow row = new ListBoxRow();
                row.Add(new Label("No events."));
                eventList.Add(row);
            }
            else
            {
                foreach (CalendarEvent ev in todayEvents)
                    eventList.Add(MakeEventRow(ev));
            }

            UpdateSelectedDate();
            eventList.ShowAll();
        }

        ListBoxRow MakeEventRow(CalendarEvent ev)
        {
            ListBoxRow row = new ListBoxRow();
            VBox box = new VBox(false, 3);
            box.BorderWidth = 5;

            Label title = new Label();
            title.Xalign = 0;
            title.Markup = "<b>" + ev.Title + "</b>";

            Label time = new Label();
            time.Xalign = 0;
            time.Text = ev.Date.ToString("HH:mm");

            Label desc = new Label();
            desc.Xalign = 0;
            desc.Text = ev.Description;

            box.PackStart(title, false, false, 0);
            box.PackStart(time, false, false, 0);
            box.PackStart(desc, false, false, 0);

            row.Add(box);
            row.Data["event"] = ev;

            return row;
        }

        void AddEvent(object sender, EventArgs e)
        {
            string title = titleEntry.Text.Trim();
            string desc = descriptionEntry.Text.Trim();

            if (title == "")
            {
                ShowMessage("Please enter event title.");
                return;
            }

            DateTime date = new DateTime(
                selectedDate.Year,
                selectedDate.Month,
                selectedDate.Day,
                hourSpin.ValueAsInt,
                minuteSpin.ValueAsInt,
                0
            );

            CalendarEvent ev = new CalendarEvent(title, desc, date);
            eventsList.Add(ev);

            titleEntry.Text = "";
            descriptionEntry.Text = "";
            hourSpin.Value = 0;
            minuteSpin.Value = 0;

            DrawCalendar();
            RefreshEvents();
        }

        void DeleteEventFromList(object sender, EventArgs e)
        {
            ListBoxRow row = eventList.SelectedRow;

            if (row == null)
            {
                ShowMessage("Select an event first.");
                return;
            }

            if (!row.Data.ContainsKey("event"))
            {
                ShowMessage("This row has no event.");
                return;
            }

            CalendarEvent ev = row.Data["event"] as CalendarEvent;
            if (ev != null) eventsList.Remove(ev);

            DrawCalendar();
            RefreshEvents();
        }

        int CountEvents(DateTime date)
        {
            int count = 0;

            foreach (CalendarEvent ev in eventsList)
                if (ev.Date.Date == date.Date)
                    count++;

            return count;
        }

        List<CalendarEvent> GetEvents(DateTime date)
        {
            List<CalendarEvent> result = new List<CalendarEvent>();

            foreach (CalendarEvent ev in eventsList)
                if (ev.Date.Date == date.Date)
                    result.Add(ev);

            return result;
        }

        void UpdateSelectedDate()
        {
            selectedDateLabel.Markup = $"<b>{selectedDate:dddd, dd MMMM yyyy}</b>";
        }

        void ShowMessage(string text)
        {
            MessageDialog dialog = new MessageDialog(
                this,
                DialogFlags.Modal,
                MessageType.Info,
                ButtonsType.Ok,
                text
            );

            dialog.Run();
            dialog.Destroy();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Application.Init();
            CalendarApp app = new CalendarApp();
            Application.Run();
        }
    }
}
