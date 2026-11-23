using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
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
    private static readonly string PastaApp = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Lembretes");
    private static readonly string ArquivoDados = Path.Combine(PastaApp, "tarefas.json");

    private readonly ObservableCollection<Tarefa> tarefas = new();

    public MainWindow()
    {
        InitializeComponent();
        Carregar();
        ListaTarefas.ItemsSource = tarefas;
    }

    private void Carregar()
    {
        try
        {
            if (!File.Exists(ArquivoDados)) return;
            var json = File.ReadAllText(ArquivoDados);
            var lista = JsonSerializer.Deserialize<List<Tarefa>>(json);
            if (lista == null) return;
            foreach (var t in lista) tarefas.Add(t);
        }
        catch (Exception)
        {
            // Atalho provisório: se o arquivo estiver quebrado, começa vazio.
        }
    }

    private void Salvar()
    {
        Directory.CreateDirectory(PastaApp);
        var json = JsonSerializer.Serialize(tarefas, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ArquivoDados, json);
    }

    private void BtnAdicionar_Click(object sender, RoutedEventArgs e)
    {
        var texto = TxtNovo.Text.Trim();
        if (texto == "") return;
        tarefas.Add(new Tarefa { Texto = texto });
        TxtNovo.Clear();
        TxtNovo.Focus();
        Salvar();
    }

    private void BtnApagar_Click(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).Tag is Tarefa t)
        {
            tarefas.Remove(t);
            Salvar();
        }
    }

    private void Chk_Click(object sender, RoutedEventArgs e) => Salvar();
}