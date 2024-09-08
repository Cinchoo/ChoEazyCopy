using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;
using System.Globalization;

namespace ChoEazyCopy
{
    public class ChoMaskedTextBox : TextBox
    {
        #region DependencyProperties

        public string UnmaskedText
        {
            get { return (string)GetValue(UnmaskedTextProperty); }
            set
            {
                SetValue(UnmaskedTextProperty, value);
            }
        }

        public static readonly DependencyProperty UnmaskedTextProperty =
        DependencyProperty.Register("UnmaskedText", typeof(string),
        typeof(ChoMaskedTextBox), new UIPropertyMetadata(""));

        public static readonly DependencyProperty InputMaskProperty =
        DependencyProperty.Register("InputMask", typeof(string), typeof(ChoMaskedTextBox), null);

        public string InputMask
        {
            get { return (string)GetValue(InputMaskProperty); }
            set { SetValue(InputMaskProperty, value); }
        }

        public static readonly DependencyProperty PromptCharProperty =
        DependencyProperty.Register("PromptChar", typeof(char), typeof(ChoMaskedTextBox),
        new PropertyMetadata('_'));

        public char PromptChar
        {
            get { return (char)GetValue(PromptCharProperty); }
            set { SetValue(PromptCharProperty, value); }
        }

        #endregion

        private MaskedTextProvider Provider;

        public ChoMaskedTextBox()
        {
            Loaded += new RoutedEventHandler(MaskedTextBox_Loaded);
            PreviewTextInput += new TextCompositionEventHandler(MaskedTextBox_PreviewTextInput);
            PreviewKeyDown += new KeyEventHandler(MaskedTextBox_PreviewKeyDown);

        }

        void MaskedTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            int position = 0;

            if (e.Key == Key.Space)
            {
                this.TreatSelectedText();

                position = this.GetNextCharacterPosition(SelectionStart, true);

                if (this.Provider.InsertAt(" ", position))
                    this.RefreshText(position);

                e.Handled = true;
            }

            if (e.Key == Key.Back)
            {
                this.TreatSelectedText();

                e.Handled = true;

                if (position > 0)
                {
                    position = this.GetNextCharacterPosition(position - 1, false);
                    if (this.Provider.RemoveAt(position))
                    {
                        if (position > 0)
                            position = this.GetNextCharacterPosition(position, false);
                    }
                }

                this.RefreshText(position);

                e.Handled = true;
            }

            if (e.Key == Key.Delete)
            {
                if (this.TreatSelectedText())
                {
                    this.RefreshText(SelectionStart);
                }
                else
                {

                    if (this.Provider.RemoveAt(SelectionStart))
                        this.RefreshText(SelectionStart);

                }

                e.Handled = true;
            }
        }

        void MaskedTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            this.TreatSelectedText();

            var position = this.GetNextCharacterPosition(SelectionStart, true);

            if (Keyboard.IsKeyToggled(Key.Insert))
            {
                if (this.Provider.Replace(e.Text, position))
                    position++;
            }
            else
            {
                if (this.Provider.InsertAt(e.Text, position))
                    position++;
            }

            position = this.GetNextCharacterPosition(position, true);

            this.RefreshText(position);

            e.Handled = true;
        }

        void MaskedTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            this.Provider = new MaskedTextProvider(InputMask, CultureInfo.CurrentCulture);

            if (String.IsNullOrWhiteSpace(UnmaskedText))
                this.Provider.Set(String.Empty);
            else
                this.Provider.Set(UnmaskedText);

            this.Provider.PromptChar = PromptChar;
            Text = this.Provider.ToDisplayString();

            var textProp = DependencyPropertyDescriptor.FromProperty(ChoMaskedTextBox.TextProperty, typeof(ChoMaskedTextBox));
            if (textProp != null)
            {
                textProp.AddValueChanged(this, (s, args) => this.UpdateText());
            }
            DataObject.AddPastingHandler(this, Pasting);
        }

        private void Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var pastedText = (string)e.DataObject.GetData(typeof(string));

                this.TreatSelectedText();

                var position = GetNextCharacterPosition(SelectionStart, true);

                if (this.Provider.InsertAt(pastedText, position))
                {
                    this.RefreshText(position);
                }
            }

            e.CancelCommand();
        }

        private void UpdateText()
        {
            if (this.Provider.ToDisplayString().Equals(Text))
                return;

            var success = this.Provider.Set(Text);

            this.SetText(success ? this.Provider.ToDisplayString() : Text, this.Provider.ToString(false, false));
        }

        private bool TreatSelectedText()
        {
            if (SelectionLength > 0)
            {
                return this.Provider.RemoveAt(SelectionStart,
                SelectionStart + SelectionLength - 1);
            }
            return false;
        }

        private void RefreshText(int position)
        {
            SetText(this.Provider.ToDisplayString(), this.Provider.ToString(false, false));
            SelectionStart = position;
        }

        private void SetText(string text, string unmaskedText)
        {
            UnmaskedText = String.IsNullOrWhiteSpace(unmaskedText) ? null : unmaskedText;
            Text = String.IsNullOrWhiteSpace(text) ? null : text;
        }

        private int GetNextCharacterPosition(int startPosition, bool goForward)
        {
            var position = this.Provider.FindEditPositionFrom(startPosition, goForward);

            if (position == -1)
                return startPosition;
            else
                return position;
        }
    }
}