// Copyright (c) 2010 Martin Knafve / hMailServer.com.  
// http://www.hmailserver.com

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace hMailServer.Administrator
{
    public partial class formApplication : Form
    {
        public formApplication()
        {
            InitializeComponent();
            Strings.Localize(this);
        }

        private void formApplication_Load(object sender, EventArgs e)
        {

        }

        
    }
}