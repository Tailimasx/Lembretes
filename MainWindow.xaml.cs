using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Lembretes;

public class Subtarefa
{
    public string Texto { get; set; } = "";
    public bool Feito { get; set; }
}

public class Tarefa
{
    public string Texto { get; set; } = "";
    public bool Feito { get; set; }
    public bool Expandida { get; set; }
    public ObservableCollection<Subtarefa> Subtarefas { get; set; } = new();
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

    private void BtnApagarSub_Click(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).Tag is Subtarefa sub)
        {
            foreach (var t in tarefas)
            {
                if (t.Subtarefas.Remove(sub)) break;
            }
            Salvar();
        }
    }

    private void TxtSub_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true; // impede que o Enter também clique no botão "Adicionar"

        var caixa = (TextBox)sender;
        var texto = caixa.Text.Trim();
        if (texto == "" || caixa.Tag is not Tarefa pai) return;

        pai.Subtarefas.Add(new Subtarefa { Texto = texto });
        caixa.Clear();
        Salvar();
    }

    // Usado pelas caixinhas de marcar e pela setinha: qualquer mudança salva.
    private void Chk_Click(object sender, RoutedEventArgs e) => Salvar();
}