using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows;

namespace ChoEazyCopy
{
    public class ChoUserPreferences
    {
        #region Public Properties

        public double WindowTop { get; set; }

        public double WindowLeft { get; set; }

        public double WindowHeight { get; set; }

        public double WindowWidth { get; set; }

        public System.Windows.WindowState WindowState { get; set; }

        public bool RememberWindowSizeAndPosition { get; set; }

        public bool ScrollOutput { get; set; }

        #endregion

        #region Constructor

        public ChoUserPreferences()
        {
            //Load the settings
            Load();

            //Size it to fit the current screen
            SizeToFit();

            //Move the window at least partially into view
            MoveIntoView();
        }

        #endregion //Constructor

        #region Functions

        /// <summary>
        /// If the saved window dimensions are larger than the current screen shrink the
        /// window to fit.
        /// </summary>
        public void SizeToFit()
        {
            if (WindowHeight > System.Windows.SystemParameters.VirtualScreenHeight)
                WindowHeight = System.Windows.SystemParameters.VirtualScreenHeight;

            if (WindowWidth > System.Windows.SystemParameters.VirtualScreenWidth)
                WindowWidth = System.Windows.SystemParameters.VirtualScreenWidth;
        }

        /// <summary>
        /// If the window is more than half off of the screen move it up and to the left 
        /// so half the height and half the width are visible.
        /// </summary>
        public void MoveIntoView()
        {
            //if (WindowTop + WindowHeight / 2 > System.Windows.SystemParameters.VirtualScreenHeight)
            //    WindowTop = System.Windows.SystemParameters.VirtualScreenHeight - WindowHeight;

            //if (WindowLeft + WindowWidth / 2 > System.Windows.SystemParameters.VirtualScreenWidth)
            //    WindowLeft = System.Windows.SystemParameters.VirtualScreenWidth - WindowWidth;

            //if (WindowTop < 0)
            //    WindowTop = 0;

            //if (WindowLeft < 0)
            //    WindowLeft = 0;

            // make sure it's in the current view space
            if (WindowTop + (WindowHeight / 2)
                > (SystemParameters.VirtualScreenHeight + SystemParameters.VirtualScreenTop))
            {
                WindowTop = SystemParameters.VirtualScreenHeight + SystemParameters.VirtualScreenTop - WindowHeight;
            }

            if (WindowLeft + (WindowWidth / 2)
                > (SystemParameters.VirtualScreenWidth + SystemParameters.VirtualScreenLeft))
            {
                WindowLeft = SystemParameters.VirtualScreenWidth + SystemParameters.VirtualScreenLeft - WindowWidth;
            }

            if (WindowTop < SystemParameters.VirtualScreenTop)
            {
                WindowTop = SystemParameters.VirtualScreenTop;
            }

            if (WindowLeft < SystemParameters.VirtualScreenLeft)
            {
                WindowLeft = SystemParameters.VirtualScreenLeft;
            }
        }

        private void Load()
        {
            WindowTop = Properties.Settings.Default.WindowTop;
            WindowLeft = Properties.Settings.Default.WindowLeft;
            WindowHeight = Properties.Settings.Default.WindowHeight;
            WindowWidth = Properties.Settings.Default.WindowWidth;
            WindowState = Properties.Settings.Default.WindowState;
            RememberWindowSizeAndPosition = Properties.Settings.Default.RememberWindowSizeAndPosition;
            ScrollOutput = Properties.Settings.Default.ScrollOutput;
        }

        public void Save()
        {
            if (WindowState != System.Windows.WindowState.Minimized)
            {
                Properties.Settings.Default.WindowTop = WindowTop;
                Properties.Settings.Default.WindowLeft = WindowLeft;
                Properties.Settings.Default.WindowHeight = WindowHeight;
                Properties.Settings.Default.WindowWidth = WindowWidth;
                Properties.Settings.Default.WindowState = WindowState;
                Properties.Settings.Default.RememberWindowSizeAndPosition = RememberWindowSizeAndPosition;
                Properties.Settings.Default.ScrollOutput = ScrollOutput;

                Properties.Settings.Default.Save();
            }
        }

        #endregion //Functions

    }
}
