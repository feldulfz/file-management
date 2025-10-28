using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.ComponentModel;

namespace UI_NSPT_Grade_Inventory.ProgressBarFolder
{
    public enum TextPosition
    {
        Left,
        Right,
        Center,
        Sliding,
        None
    }

    internal class ProgressBarClass : ProgressBar
    {
        //Fields
        private Color channelColor = Color.LightSteelBlue; //channel color for progress bar
        private Color sliderColor = Color.RoyalBlue; //color for the slider
        private Color foreBackColor = Color.RoyalBlue; //background color of the text
        private int channelHeight = 6; //a field to set the height of the channel
        private int sliderHeight = 6; //a field to set the height of the slider

        private TextPosition showValue = TextPosition.Right; //text position to show the value
        private string symbolBefore = "";
        private string symbolAfter = "";
        private bool showMaximun = false;

        //Other field to avoid flickering of the progress bar
        private bool paintedBack = false; // a field to determine if the background has been painted
        private bool stopPainting = false; // a field to stop painting

        //Constructor
        public ProgressBarClass()
        {
            this.SetStyle(ControlStyles.UserPaint, true); //specify the style and behaviour of the control, in this case the control will be painted by user and not the operating system
            this.ForeColor = Color.White; //set the default text color
        }

        //Properties
        [Category("RJ Code Advance")]
        public Color ChannelColor
        {
            get
            {
                return channelColor;
            }
            set
            {
                channelColor = value;
                this.Invalidate(); // Invoke the Invalidate() method to repaint the control and thus update the appearance
            }
        }

        [Category("RJ Code Advance")]
        public Color SliderColor
        {
            get
            {
                return sliderColor;
            }
            set
            {
                sliderColor = value;
                this.Invalidate(); // Invoke the Invalidate() method to repaint the control and thus update the appearance
            }
        }

        [Category("RJ Code Advance")]
        public Color ForeBackColor
        {
            get
            {
                return foreBackColor;
            }
            set
            {
                foreBackColor = value;
                this.Invalidate(); // Invoke the Invalidate() method to repaint the control and thus update the appearance
            }
        }

        [Category("RJ Code Advance")]
        public int ChannelHeight
        {
            get
            {
                return channelHeight;
            }
            set
            {
                channelHeight = value;
                this.Invalidate(); // Invoke the Invalidate() method to repaint the control and thus update the appearance
            }
        }

        [Category("RJ Code Advance")]
        public int SliderHeight
        {
            get
            {
                return sliderHeight;
            }
            set
            {
                sliderHeight = value;
                this.Invalidate(); // Invoke the Invalidate() method to repaint the control and thus update the appearance
            }
        }

        [Category("RJ Code Advance")]
        public TextPosition ShowValue
        {
            get
            {
                return showValue;
            }
            set
            {
                showValue = value;
                this.Invalidate(); // Invoke the Invalidate() method to repaint the control and thus update the appearance
            }
        }

        [Category("RJ Code Advance")]
        public string SymbolBefore
        {
            get { return symbolBefore; }
            set
            {
                symbolBefore = value;
                this.Invalidate();
            }
        }

        [Category("RJ Code Advance")]
        public string SymbolAfter
        {
            get { return symbolAfter; }
            set
            {
                symbolAfter = value;
                this.Invalidate();
            }
        }

        [Category("RJ Code Advance")]
        public bool ShowMaximun
        {
            get { return showMaximun; }
            set
            {
                showMaximun = value;
                this.Invalidate();
            }
        }

        //Override the font property because it is hidden in the toolbox code editor, then show it again through:
        // [Browsable(true)]
        // [EditorBrowsable(EditorBrowsableState.Always)]

        [Category("RJ Code Advance")]
        [Browsable(true)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        public override Font Font  //Override the font property because it is hidden in the toolbox code editor
        {
            get
            {
                return base.Font;
            }
            set
            {
                base.Font = value;
            }
        }


        //Optionally we override the forecolor property to group the properties in a single category and
        //thus be able to easily locate them in the properties blocks
        [Category("RJ Code Advance")]
        public override Color ForeColor
        {
            get
            {
                return base.ForeColor;
            }
            set
            {
                base.ForeColor = value;
            }
        }

        //-> Paint the background & channel
        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            if (stopPainting == false) // to avoid flickering
            {
                if (paintedBack == false) // to avoid flickering
                {
                    //Fields
                    Graphics graph = pevent.Graphics; //for drawing and painting
                    Rectangle rectChannel = new Rectangle(0, 0, this.Width, ChannelHeight); //rectangle for channel dimensions 
                    using (var brushChannel = new SolidBrush(channelColor)) //a solid brush to paint the channel
                    {
                        if (channelHeight >= sliderHeight)
                            rectChannel.Y = this.Height - channelHeight;
                        else rectChannel.Y = this.Height - ((channelHeight + sliderHeight) / 2);

                        //Painting
                        graph.Clear(this.Parent.BackColor);//Surface
                        graph.FillRectangle(brushChannel, rectChannel);//Channel

                        //Stop painting the back & channel
                        if (this.DesignMode == false)
                        {
                            paintedBack = true;
                        }
                    }
                }
                //Reset painting the back & channel
                if (this.Value == this.Maximum || this.Value == this.Minimum)
                    paintedBack = false;
            }
        }

        //-> Paint slider
        protected override void OnPaint(PaintEventArgs e)
        {
            if (stopPainting == false)//check if the painting was not stop
            {
                //Fields
                Graphics graph = e.Graphics; //for drawing and painting
                double scaleFactor = (((double)this.Value - this.Minimum) / ((double)this.Maximum - this.Minimum)); //to obtain the scale factor of progress bar
                int sliderWidth = (int)(this.Width * scaleFactor);
                Rectangle rectSlider = new Rectangle(0, 0, sliderWidth, sliderHeight); //declare a rectangle for the dimension of the slider                                                                                       
                using (var brushSlider = new SolidBrush(sliderColor)) // a solid brush to paint the slide
                {
                    if (sliderHeight >= channelHeight)
                        rectSlider.Y = this.Height - sliderHeight;
                    else rectSlider.Y = this.Height - ((sliderHeight + channelHeight) / 2);

                    //Painting
                    if (sliderWidth > 1) //Slider
                        graph.FillRectangle(brushSlider, rectSlider);
                    if (showValue != TextPosition.None) //Text
                        DrawValueText(graph, sliderWidth, rectSlider);
                }
            }
            if (this.Value == this.Maximum) stopPainting = true;//Stop painting
            else stopPainting = false; //Keep painting
        }

        //-> Paint value text
        private void DrawValueText(Graphics graph, int sliderWidth, Rectangle rectSlider)
        {
            //Fields
            string text = symbolBefore + this.Value.ToString() + symbolAfter;
            if (showMaximun) text = text + "/" + symbolBefore + this.Maximum.ToString() + symbolAfter;
            var textSize = TextRenderer.MeasureText(text, this.Font);
            var rectText = new Rectangle(0, 0, textSize.Width, textSize.Height + 2);
            using (var brushText = new SolidBrush(this.ForeColor))
            using (var brushTextBack = new SolidBrush(foreBackColor))
            using (var textFormat = new StringFormat())
            {
                switch (showValue)
                {
                    case TextPosition.Left:
                        rectText.X = 0;
                        textFormat.Alignment = StringAlignment.Near;
                        break;
                    case TextPosition.Right:
                        rectText.X = this.Width - textSize.Width;
                        textFormat.Alignment = StringAlignment.Far;
                        break;
                    case TextPosition.Center:
                        rectText.X = (this.Width - textSize.Width) / 2;
                        textFormat.Alignment = StringAlignment.Center;
                        break;
                    case TextPosition.Sliding:
                        rectText.X = sliderWidth - textSize.Width;
                        textFormat.Alignment = StringAlignment.Center;
                        //Clean previous text surface
                        using (var brushClear = new SolidBrush(this.Parent.BackColor))
                        {
                            var rect = rectSlider;
                            rect.Y = rectText.Y;
                            rect.Height = rectText.Height;
                            graph.FillRectangle(brushClear, rect);
                        }
                        break;
                }
                //Painting
                graph.FillRectangle(brushTextBack, rectText);
                graph.DrawString(text, this.Font, brushText, rectText, textFormat);
            }
        }


    }
}
