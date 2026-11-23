using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace Lembretes;

public class Tarefa
{
    public string Texto { get; set; } = "";
    public bool Feito { get; set; }
}

public partial class MainWindow : Window
{
    private readonly ObservableCollection<Tarefa> tarefas = new();

    public MainWindow()
    {
        InitializeComponent();
        ListaTarefas.ItemsSource = tarefas;
    }

    private void BtnAdicionar_Click(object sender, RoutedEventArgs e)
    {
        var texto = TxtNovo.Text.Trim();
        if (texto == "") return;
        tarefas.Add(new Tarefa { Texto = texto });
        TxtNovo.Clear();
        TxtNovo.Focus();
    }

    private void BtnApagar_Click(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).Tag is Tarefa t) tarefas.Remove(t);
    }
}