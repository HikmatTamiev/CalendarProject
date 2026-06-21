using Gtk;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using static System.Net.Mime.MediaTypeNames;

namespace SimpleCalendarGtk
{
    /// <summary>
    /// Represents a calendar event with a title, description, date, and repeat setting.
    /// </summary>
    class CalendarEvent
    {
        /// <summary>The title of the event.</summary>
        public string Title;

        /// <summary>A short description of the event.</summary>
        public string Description;

        /// <summary>The date and time the event occurs.</summary>
        public DateTime Date;

        /// <summary>The repeat interval: "None", "Daily", "Weekly", or "Yearly".</summary>
        public string Repeat;

        /// <summary>
        /// Creates a new calendar event.
        /// </summary>
        /// <param name="title">The event title.</param>
        /// <param name="desc">A short description.</param>
        /// <param name="date">The date and time of the event.</param>
        /// <param name="repeat">How often the event repeats.</param>
        public CalendarEvent(string title, string desc, DateTime date, string repeat)
        {
            Title = title;
            Description = desc;
            Date = date;
            Repeat = repeat;
        }

        /// <summary>
        /// Checks whether this event occurs on a given day, taking repeat settings into account.
        /// </summary>
        /// <param name="day">The day to check.</param>
        /// <returns>True if the event occurs on that day, false otherwise.</returns>
        public bool OccursOn(DateTime day)
        {
            if (Repeat == "None") return Date.Date == day.Date;
            if (day.Date < Date.Date) return false;
            if (Repeat == "Daily") return true;
            if (Repeat == "Weekly") return Date.DayOfWeek == day.DayOfWeek;
            if (Repeat == "Yearly") return Date.Day == day.Day && Date.Month == day.Month;
            return false;
        }
    }

    /// <summary>
    /// The main application window. Displays a monthly calendar and allows
    /// the user to add, view, and delete events.
    /// </summary>
    class CalendarApp : Window
    {
        DateTime currentMonth = DateTime.Today;
        DateTime selectedDate = DateTime.Today;

        Gtk.Label monthLabel, selectedDateLabel;
        Grid calendarGrid;
        ListBox eventList;
        Entry titleEntry, descriptionEntry;
        SpinButton hourSpin, minuteSpin;
        ComboBoxText repeatCombo;
        List<CalendarEvent> eventsList = new List<CalendarEvent>();
        string fileName = "events.txt";

        /// <summary>
        /// Initializes the calendar window, loads saved events, and builds the UI.
        /// </summary>
        public CalendarApp() : base("Mini Calendar")
        {
            SetDefaultSize(850, 600);
            SetPosition(WindowPosition.Center);
            DeleteEvent += delegate { SaveEvents(); Gtk.Application.Quit(); };

            LoadEvents();

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

        /// <summary>
        /// Creates the top navigation bar with previous, next, and today buttons.
        /// </summary>
        HBox CreateHeader()
        {
            HBox box = new HBox(false, 10);

            Button prev = new Button("<");
            Button next = new Button(">");
            Button today = new Button("Today");

            monthLabel = new Gtk.Label();
            monthLabel.SetSizeRequest(250, 30);

            prev.Clicked += delegate { currentMonth = currentMonth.AddMonths(-1); DrawCalendar(); };
            next.Clicked += delegate { currentMonth = currentMonth.AddMonths(1); DrawCalendar(); };
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

        /// <summary>
        /// Creates the left panel containing the monthly calendar grid.
        /// </summary>
        VBox CreateCalendarPart()
        {
            VBox box = new VBox(false, 8);

            Gtk.Label title = new Gtk.Label();
            title.Markup = "<b>Calendar</b>";

            calendarGrid = new Grid();
            calendarGrid.RowSpacing = 4;
            calendarGrid.ColumnSpacing = 4;

            box.PackStart(title, false, false, 0);
            box.PackStart(calendarGrid, true, true, 0);

            return box;
        }

        /// <summary>
        /// Creates the right panel with the event list and form for adding new events.
        /// </summary>
        VBox CreateEventPart()
        {
            VBox box = new VBox(false, 8);
            box.SetSizeRequest(280, -1);

            selectedDateLabel = new Gtk.Label();

            Gtk.Label listTitle = new Gtk.Label();
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

            repeatCombo = new ComboBoxText();
            repeatCombo.AppendText("None");
            repeatCombo.AppendText("Daily");
            repeatCombo.AppendText("Weekly");
            repeatCombo.AppendText("Yearly");
            repeatCombo.Active = 0;

            HBox timeBox = new HBox(false, 5);
            timeBox.PackStart(new Gtk.Label("Hour"), false, false, 0);
            timeBox.PackStart(hourSpin, false, false, 0);
            timeBox.PackStart(new Gtk.Label("Minute"), false, false, 0);
            timeBox.PackStart(minuteSpin, false, false, 0);

            Button addButton = new Button("Add Event");
            Button deleteButton = new Button("Delete Selected");

            addButton.Clicked += AddEvent;
            deleteButton.Clicked += DeleteEventFromList;

            box.PackStart(selectedDateLabel, false, false, 0);
            box.PackStart(listTitle, false, false, 0);
            box.PackStart(scroll, true, true, 0);
            box.PackStart(new Gtk.Label("Add new event"), false, false, 0);
            box.PackStart(titleEntry, false, false, 0);
            box.PackStart(descriptionEntry, false, false, 0);
            box.PackStart(timeBox, false, false, 0);
            box.PackStart(new Gtk.Label("Repeat"), false, false, 0);
            box.PackStart(repeatCombo, false, false, 0);
            box.PackStart(addButton, false, false, 0);
            box.PackStart(deleteButton, false, false, 0);
            return box;
        }

        /// <summary>
        /// Redraws the calendar grid for the current month.
        /// </summary>
        void DrawCalendar()
        {
            foreach (Widget child in calendarGrid.Children) calendarGrid.Remove(child);

            monthLabel.Markup = $"<b>{currentMonth:MMMM yyyy}</b>";
            string[] days = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };

            for (int i = 0; i < days.Length; i++)
            {
                Gtk.Label label = new Gtk.Label();
                label.Markup = $"<b>{days[i]}</b>";
                calendarGrid.Attach(label, i, 0, 1, 1);
            }

            DateTime first = new DateTime(currentMonth.Year, currentMonth.Month, 1);
            int column = GetColumn(first);
            int row = 1;
            int daysInMonth = DateTime.DaysInMonth(currentMonth.Year, currentMonth.Month);

            for (int day = 1; day <= daysInMonth; day++)
            {
                DateTime date = new DateTime(currentMonth.Year, currentMonth.Month, day);
                Button button = MakeDayButton(date);

                calendarGrid.Attach(button, column, row, 1, 1);

                column++;
                if (column == 7) { column = 0; row++; }
            }

            UpdateSelectedDate();
            calendarGrid.ShowAll();
        }

        /// <summary>
        /// Creates a clickable button for a single day in the calendar grid.
        /// Shows the day number and event count if any events exist.
        /// </summary>
        /// <param name="date">The date this button represents.</param>
        Button MakeDayButton(DateTime date)
        {
            VBox box = new VBox(false, 2);

            Gtk.Label number = new Gtk.Label(date.Day.ToString());
            Gtk.Label info = new Gtk.Label();

            int count = CountEvents(date);
            info.Text = count > 0 ? count + " event(s)" : "";

            box.PackStart(number, false, false, 0);
            box.PackStart(info, false, false, 0);

            Button button = new Button();
            button.SetSizeRequest(85, 65);
            button.Add(box);

            if (date.Date == DateTime.Today.Date) button.TooltipText = "Today";

            button.Clicked += delegate
            {
                selectedDate = date;
                DrawCalendar();
                RefreshEvents();
            };

            return button;
        }

        /// <summary>
        /// Returns the grid column index (0 = Monday, 6 = Sunday) for a given date.
        /// </summary>
        /// <param name="date">The date to check.</param>
        int GetColumn(DateTime date)
        {
            int day = (int)date.DayOfWeek;
            if (day == 0) return 6;
            return day - 1;
        }

        /// <summary>
        /// Refreshes the event list panel to show events for the selected date.
        /// </summary>
        void RefreshEvents()
        {
            foreach (Widget child in eventList.Children) eventList.Remove(child);

            List<CalendarEvent> todayEvents = GetEvents(selectedDate);
            todayEvents.Sort((a, b) => a.Date.CompareTo(b.Date));

            if (todayEvents.Count == 0)
            {
                ListBoxRow row = new ListBoxRow();
                row.Add(new Gtk.Label("No events."));
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

        /// <summary>
        /// Creates a list row widget displaying the details of a single event.
        /// </summary>
        /// <param name="ev">The event to display.</param>
        ListBoxRow MakeEventRow(CalendarEvent ev)
        {
            ListBoxRow row = new ListBoxRow();
            VBox box = new VBox(false, 3);
            box.BorderWidth = 5;

            Gtk.Label title = new Gtk.Label();
            title.Xalign = 0;
            title.Markup = "<b>" + ev.Title + "</b>";

            Gtk.Label time = new Gtk.Label();
            time.Xalign = 0;
            time.Text = ev.Date.ToString("HH:mm") + " | Repeat: " + ev.Repeat;

            Gtk.Label desc = new Gtk.Label();
            desc.Xalign = 0;
            desc.Text = ev.Description;

            box.PackStart(title, false, false, 0);
            box.PackStart(time, false, false, 0);
            box.PackStart(desc, false, false, 0);

            row.Add(box);
            row.Data["event"] = ev;

            return row;
        }

        /// <summary>
        /// Handles the Add Event button click. Validates input, creates a new event,
        /// and saves it to disk.
        /// </summary>
        void AddEvent(object sender, EventArgs e)
        {
            string title = titleEntry.Text.Trim();
            string desc = descriptionEntry.Text.Trim();

            if (title == "") { ShowMessage("Please enter event title."); return; }

            DateTime date = new DateTime(
                selectedDate.Year, selectedDate.Month, selectedDate.Day,
                hourSpin.ValueAsInt, minuteSpin.ValueAsInt, 0
            );

            string repeat = repeatCombo.ActiveText;
            CalendarEvent ev = new CalendarEvent(title, desc, date, repeat);
            eventsList.Add(ev);

            titleEntry.Text = "";
            descriptionEntry.Text = "";
            hourSpin.Value = 0;
            minuteSpin.Value = 0;
            repeatCombo.Active = 0;

            SaveEvents();
            DrawCalendar();
            RefreshEvents();
        }

        /// <summary>
        /// Handles the Delete Selected button click. Removes the selected event
        /// from the list and saves the updated list to disk.
        /// </summary>
        void DeleteEventFromList(object sender, EventArgs e)
        {
            ListBoxRow row = eventList.SelectedRow;

            if (row == null) { ShowMessage("Select an event first."); return; }
            if (!row.Data.ContainsKey("event")) { ShowMessage("This row has no event."); return; }

            CalendarEvent ev = row.Data["event"] as CalendarEvent;
            if (ev != null) eventsList.Remove(ev);

            SaveEvents();
            DrawCalendar();
            RefreshEvents();
        }

        /// <summary>
        /// Counts how many events occur on a given date.
        /// </summary>
        /// <param name="date">The date to check.</param>
        int CountEvents(DateTime date)
        {
            int count = 0;
            foreach (CalendarEvent ev in eventsList)
                if (ev.OccursOn(date)) count++;
            return count;
        }

        /// <summary>
        /// Returns all events that occur on a given date.
        /// </summary>
        /// <param name="date">The date to filter by.</param>
        List<CalendarEvent> GetEvents(DateTime date)
        {
            List<CalendarEvent> result = new List<CalendarEvent>();
            foreach (CalendarEvent ev in eventsList)
                if (ev.OccursOn(date)) result.Add(ev);
            return result;
        }

        /// <summary>
        /// Saves all events to a text file, one event per line in pipe-delimited format.
        /// </summary>
        void SaveEvents()
        {
            try
            {
                List<string> lines = new List<string>();

                foreach (CalendarEvent ev in eventsList)
                {
                    string line = ev.Date.Ticks + "|" + ev.Repeat + "|" +
                                  Clean(ev.Title) + "|" + Clean(ev.Description);
                    lines.Add(line);
                }

                File.WriteAllLines(fileName, lines);
            }
            catch
            {
                ShowMessage("Could not save events.");
            }
        }

        /// <summary>
        /// Loads events from the text file on disk into the events list.
        /// Skips malformed lines and handles missing or corrupt files gracefully.
        /// </summary>
        void LoadEvents()
        {
            try
            {
                if (!File.Exists(fileName)) return;

                string[] lines = File.ReadAllLines(fileName);

                foreach (string line in lines)
                {
                    try
                    {
                        string[] parts = line.Split('|');
                        if (parts.Length != 4) continue;

                        DateTime date = new DateTime(long.Parse(parts[0]));
                        string repeat = parts[1];
                        string title = parts[2];
                        string desc = parts[3];

                        if (!IsValidRepeat(repeat)) repeat = "None";

                        eventsList.Add(new CalendarEvent(title, desc, date, repeat));
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            catch
            {
                ShowMessage("Event file is missing or corrupt. Starting with empty calendar.");
                eventsList.Clear();
            }
        }

        /// <summary>
        /// Sanitizes a string for safe storage by removing pipe characters and newlines.
        /// </summary>
        /// <param name="text">The text to clean.</param>
        string Clean(string text)
        {
            return text.Replace("|", "/").Replace("\n", " ");
        }

        /// <summary>
        /// Checks whether a repeat string is one of the accepted values.
        /// </summary>
        /// <param name="repeat">The repeat value to validate.</param>
        bool IsValidRepeat(string repeat)
        {
            return repeat == "None" || repeat == "Daily" ||
                   repeat == "Weekly" || repeat == "Yearly";
        }

        /// <summary>
        /// Updates the selected date label to show the currently selected date.
        /// </summary>
        void UpdateSelectedDate()
        {
            selectedDateLabel.Markup = $"<b>{selectedDate:dddd, dd MMMM yyyy}</b>";
        }

        /// <summary>
        /// Shows a modal info dialog with a message.
        /// </summary>
        /// <param name="text">The message to display.</param>
        void ShowMessage(string text)
        {
            MessageDialog dialog = new MessageDialog(
                this, DialogFlags.Modal, MessageType.Info, ButtonsType.Ok, text
            );

            dialog.Run();
            dialog.Destroy();
        }
    }

    /// <summary>
    /// Entry point for the application.
    /// </summary>
    class Program
    {
        /// <summary>
        /// Initializes GTK and launches the calendar window.
        /// </summary>
        static void Main(string[] args)
        {
            Gtk.Application.Init();
            CalendarApp app = new CalendarApp();
            Gtk.Application.Run();
        }
    }
}
