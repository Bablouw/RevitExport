using System;
using System.Windows.Forms;

namespace RevitExport.Services
{
    public partial class UpdateProgressForm : Form
    {
        public UpdateProgressForm()
        {
            InitializeComponent();
        }

        public void UpdateProgress(int completed, int total)
        {
            
            progressBar.Value = (int)((double)completed / total * 100);
            textBox.SelectionLength = 0;
            textBox.Text = $"Обработано: {completed} из {total}.";
            textBox1.Text =$"При появлении предупреждения от Revit, " +
                $"нажмите Отмена." +
                $"\nПроблемный элемент будет проигнорирован.";
            this.Refresh();
        }

        private void progressBar1_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }

}
