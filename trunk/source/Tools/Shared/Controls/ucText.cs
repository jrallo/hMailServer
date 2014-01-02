// Copyright (c) 2010 Martin Knafve / hMailServer.com.  
// http://www.hmailserver.com

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace hMailServer.Shared
{
   public partial class ucText : TextBox, IPropertyEditor
   {
      private string internalText;
      private bool _numeric;
	  private bool bolAllowDec;    //sets the box can accept decimal numbers, when _numeric is set
	  private bool bolAllowNeg;    //sets the box can accept negative numbers, when _numeric is set
      public ucText()
      {
         internalText = "";
         _numeric = false;
		 bolAllowNeg = false;
		 bolAllowDec = false;
      }

      public new string Text
      {
         get
         {
            return base.Text.Trim();
         }

         set
         {
            base.Text = value;
            internalText = value;
         }
      }

      public int Number
      {
         get
         {
            if (_numeric == false)
               return 0;

            if (Text == "")
               return 0;

            return Convert.ToInt32(Text);
         }
         set
         {
            if (_numeric == false)
               return;

            Text = value.ToString();
         }
      }

      public Int64 Number64
      {
         get
         {
            if (_numeric == false)
               return 0;

            if (Text == "")
               return 0;

            return Convert.ToInt64(Text);
         }
         set
         {
            if (_numeric == false)
               return;

            Text = value.ToString();
         }
      }

	   /// <summary>
	   /// Gets and returns the number as a float.
	   /// </summary>
	  public float NumberFloat
	  {
		  get
		  {
			  // is it a numeric and do we have bolAllowDec?
			  if (_numeric == false || bolAllowDec == false)
				  return 0f;

			  // is the box blank?
			  if (Text == "")
				  return 0;

			  // correct way, parse it and if it fails will return an exception.
			  return float.Parse(Text);
		  }

		  set
		  {
			  // make sure this is a numeric box
			  if (_numeric == false || bolAllowDec == false)
				  return;

			  // set the text
			  Text = value.ToString();
		  }

	  }

	   /// <summary>
	   /// Sets the text box is dirty.
	   /// </summary>
      public bool Dirty
      {
         get
         {
            if (base.Text != internalText)
               return true;
            else
               return false;

         }
      }

      public void SetClean()
      {
         internalText = base.Text;
      }

	   /// <summary>
	   /// Gets and sets the text box is a numeric type
	   /// </summary>
      public bool Numeric
      {
         get
         {
            return _numeric;
         }
         set
         {
            _numeric = value;
         }
      }

	   /// <summary>
	   /// Public property to set the text box as a negative accepting box.
	   /// </summary>
	  public bool Negative
	  {
		  get { return bolAllowNeg; }
		  set { bolAllowNeg = value; }
	  }

	   /// <summary>
	   /// Public property to set the text box as a decimal accepting box.
	   /// </summary>
	  public bool Decimal
	  {
		  get { return bolAllowDec; }
		  set { bolAllowDec = value; }
	  }

	   /// <summary>
	   /// Fires when a key is pressed for the text box.
	   /// </summary>
	   /// <param name="e"></param>
      protected override void OnKeyPress(KeyPressEventArgs e)
      {
         if (_numeric)
         {
			 // single decimal point allowed
			 if ((e.KeyChar == '.') && (bolAllowDec == true))
			 {
				 if (base.Text.Contains("."))
				 {
					 e.Handled = true;
				 }
				 return;
			 }

			 // single negative sign allowed
			 if (e.KeyChar == '-' && bolAllowNeg == true)
			 {
				 if (base.Text.Contains("-"))
				 {
					 e.Handled = true;
				 }

				 // make sure the negative sign is the first digit
				 if (base.SelectionStart != 0)
				 {
					 e.Handled = true;
				 }
				 return;
			 }

			 

			 // digits and backspace only
			 if (!Char.IsDigit(e.KeyChar) && e.KeyChar != 0x08)
			 {
				 e.Handled = true;
			 }

			 // make sure there is not a number in front of the negative
			 if ((bolAllowNeg == true)  && (base.Text.Contains("-")) && (base.SelectionStart < 1))
			 {
				 e.Handled = true;
			 }
         }

         base.OnKeyPress(e);
      }

   }
}
