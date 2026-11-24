using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Lembretes;

// Classe base: avisa a tela sempre que uma propriedade muda.
// É isso que faz o texto riscar na hora quando "Feito" vira true.
public class ObjetoObservavel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    // [CallerMemberName] preenche sozinho o nome da propriedade que chamou
    protected void Avisar([CallerMemberName] string? nome = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nome));
}

//uma subtarefa: só tem texto e se está feita.
public class Subtarefa : ObjetoObservavel
{
    private string _texto = "";
    private bool _feito;

    public string Texto { get => _texto; set { _texto = value; Avisar(); } }
    public bool Feito { get => _feito; set { _feito = value; Avisar(); } }
}

//uma tarefa: texto, se está feita, se está aberta (mostrando subtarefas) e a lista de subtarefas.
public class Tarefa : ObjetoObservavel
{
    private string _texto = "";
    private bool _feito;
    private bool _expandida;

    public string Texto { get => _texto; set { _texto = value; Avisar(); } }
    public bool Feito { get => _feito; set { _feito = value; Avisar(); } }
    public bool Expandida { get => _expandida; set { _expandida = value; Avisar(); } }

    // ObservableCollection = lista que avisa a tela quando entra/sai item.
    public ObservableCollection<Subtarefa> Subtarefas { get; set; } = new();
}

//JANELA PRINCIPAL
public partial class MainWindow : Window
{
    //onde o arquivo de dados fica: %AppData%\Lembretes\tarefas.json
    private static readonly string PastaApp = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Lembretes");
    private static readonly string ArquivoDados = Path.Combine(PastaApp, "tarefas.json");

    //a lista de tarefas na memória (a tela mostra o que está aqui dentro).
    private readonly ObservableCollection<Tarefa> tarefas = new();

    public MainWindow()
    {
        InitializeComponent();   // monta a janela a partir do XAML

        //mostra a data de hoje no cabeçalho (ex.: "segunda-feira, 23 de novembro")
        TxtData.Text = DateTime.Now.ToString("dddd, d 'de' MMMM", new CultureInfo("pt-BR"));

        // posiciona no canto inferior direito, acima da barra de tarefas.
        // WorkArea = área útil da tela (sem a barra de tarefas).
        // O -4 compensa a margem de 12 que a janela tem para a sombra.
        var area = SystemParameters.WorkArea;
        Left = area.Right - Width - 4;
        Top = area.Bottom - Height - 4;

        Carregar();                           // lê o arquivo salvo
        ListaTarefas.ItemsSource = tarefas;   // liga a lista da tela às tarefas
    }

    // SALVAR E CARREGAR (JSON)

    private void Carregar()
    {
        try
        {
            if (!File.Exists(ArquivoDados)) return;   // primeira vez: não há nada salvo
            var json = File.ReadAllText(ArquivoDados);
            var lista = JsonSerializer.Deserialize<List<Tarefa>>(json);   // texto -> objetos
            if (lista == null) return;
            foreach (var t in lista) tarefas.Add(t);
        }
        catch (Exception)
        {
            // Atalho provisório: se o arquivo estiver quebrado, começa vazio.
            // Em versão futura, vamos avisar o usuário e oferecer abrir o arquivo.
        }
    }

    private void Salvar()
    {
        Directory.CreateDirectory(PastaApp);   // cria a pasta se ainda não existir
        var json = JsonSerializer.Serialize(tarefas, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ArquivoDados, json);
    }

    // ADICIONAR

    //adiciona a tarefa digitada no campo do rodapé (botão "+" ou Enter).
    private void BtnAdicionar_Click(object sender, RoutedEventArgs e)
    {
        var texto = TxtNovo.Text.Trim();
        if (texto == "") return;               //não adiciona tarefa vazia
        tarefas.Add(new Tarefa { Texto = texto });
        TxtNovo.Clear();
        TxtNovo.Focus();
        Salvar();
    }

    //Enter no campo do rodapé = adicionar.
    private void TxtNovo_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;                      // "já tratei essa tecla"
        BtnAdicionar_Click(sender, e);
    }

    //Enter no campo de subtarefa = adicionar subtarefa na tarefa dona do campo.
    private void TxtSub_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;

        var caixa = (TextBox)sender;
        var texto = caixa.Text.Trim();
        //Tag guarda a tarefa "dona" deste campo (veja Tag="{Binding}" no XAML)
        if (texto == "" || caixa.Tag is not Tarefa pai) return;

        pai.Subtarefas.Add(new Subtarefa { Texto = texto });
        caixa.Clear();
        Salvar();
    }

    // EDITAR O NOME (clicando no texto da tarefa/subtarefa)

    // Enter confirma; Esc cancela e volta ao texto anterior.
    private void TextoEditavel_KeyDown(object sender, KeyEventArgs e)
    {
        var caixa = (TextBox)sender;

        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            Keyboard.ClearFocus();   //tirar o foco dispara a confirmação (método abaixo)
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            // UpdateTarget = recarrega o texto original do objeto para a caixa (desfaz a edição)
            caixa.GetBindingExpression(TextBox.TextProperty)?.UpdateTarget();
            Keyboard.ClearFocus();
        }
    }

    //quando o campo perde o foco: grava o novo texto (ou desfaz se ficou vazio).
    private void TextoEditavel_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        var caixa = (TextBox)sender;
        var ligacao = caixa.GetBindingExpression(TextBox.TextProperty);

        if (string.IsNullOrWhiteSpace(caixa.Text))
        {
            ligacao?.UpdateTarget();   //vazio não vale: volta ao texto anterior
        }
        else
        {
            ligacao?.UpdateSource();   //grava o texto novo dentro da Tarefa/Subtarefa
            Salvar();
        }
    }

    // EXCLUIR (com animação)

    // Efeito: o item esmaece e encolhe em 0,22s; só DEPOIS é removido de verdade.
    private void RemoverComAnimacao(FrameworkElement? elemento, Action remover)
    {
        // Se não achamos o elemento na tela, remove direto, sem animação.
        if (elemento == null || elemento.ActualHeight <= 0)
        {
            remover();
            return;
        }

        var duracao = TimeSpan.FromMilliseconds(220);
        var esmaecer = new DoubleAnimation(1, 0, duracao);
        var encolher = new DoubleAnimation(elemento.ActualHeight, 0, duracao)
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }   //começa rápido, termina macio
        };

        //quando a animação termina, remove o item e solta as animações.
        encolher.Completed += (_, _) =>
        {
            remover();
            elemento.BeginAnimation(OpacityProperty, null);
            elemento.BeginAnimation(HeightProperty, null);
        };

        elemento.BeginAnimation(OpacityProperty, esmaecer);
        elemento.BeginAnimation(HeightProperty, encolher);
    }

    //lixeira de uma TAREFA.
    private void BtnApagar_Click(object sender, RoutedEventArgs e)
    {
        var botao = (Button)sender;
        if (botao.Tag is not Tarefa tarefa) return;
        botao.IsEnabled = false;   // evita clicar duas vezes durante a animação

        //acha o "recipiente" visual desta tarefa dentro da lista para animar.
        var recipiente = ListaTarefas.ItemContainerGenerator.ContainerFromItem(tarefa) as FrameworkElement;

        RemoverComAnimacao(recipiente, () =>
        {
            tarefas.Remove(tarefa);
            Salvar();
        });
    }

    //lixeira de uma SUBTAREFA.
    private void BtnApagarSub_Click(object sender, RoutedEventArgs e)
    {
        var botao = (Button)sender;
        if (botao.Tag is not Subtarefa sub) return;
        botao.IsEnabled = false;

        //o "pai" do botão é a linha (Grid) da subtarefa: é ela que vamos animar.
        var linha = botao.Parent as FrameworkElement;

        RemoverComAnimacao(linha, () =>
        {
            //procura em qual tarefa essa subtarefa está e remove de lá.
            foreach (var t in tarefas)
            {
                if (t.Subtarefas.Remove(sub)) break;
            }
            Salvar();
        });
    }

    // ARRASTAR PARA MUDAR A ORDEM DAS TAREFAS

    private Point inicioArraste;        // onde o mouse foi pressionado
    private Tarefa? tarefaCandidata;    // tarefa sob o mouse; só vira "arraste" se o mouse se mover

    //devolve o "pai" de um elemento da tela (funciona para elementos visuais e para textos soltos).
    private static DependencyObject? Pai(DependencyObject o)
        => o is Visual ? VisualTreeHelper.GetParent(o) : LogicalTreeHelper.GetParent(o);

    // 1) Mouse pressionado: descobre se foi sobre uma tarefa (e não sobre um campo ou botão).
    private void Lista_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        tarefaCandidata = null;

        // Sobe na árvore visual a partir do ponto clicado até achar o item da lista.
        var atual = e.OriginalSource as DependencyObject;
        while (atual != null && atual is not ListBoxItem)
        {
            // Clique em campo de texto, botão ou caixinha NÃO é arraste (é edição/clique normal).
            if (atual is TextBoxBase || atual is ButtonBase) return;
            atual = Pai(atual);
        }

        if (atual is ListBoxItem item && item.DataContext is Tarefa tarefa)
        {
            tarefaCandidata = tarefa;
            inicioArraste = e.GetPosition(null);
        }
    }

    // 2) Mouse se movendo com o botão pressionado: começa o arraste.
    private void Lista_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || tarefaCandidata == null) return;

        // Só começa depois de mover alguns pixels (evita arrastar sem querer ao clicar).
        var delta = e.GetPosition(null) - inicioArraste;
        if (Math.Abs(delta.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(delta.Y) < SystemParameters.MinimumVerticalDragDistance) return;

        var tarefa = tarefaCandidata;
        tarefaCandidata = null;

        DefinirOpacidade(tarefa, 0.45);   // deixa o item meio "fantasma" enquanto arrasta
        // DoDragDrop fica "preso" aqui até você soltar o mouse.
        DragDrop.DoDragDrop(ListaTarefas, new DataObject(typeof(Tarefa), tarefa), DragDropEffects.Move);
        DefinirOpacidade(tarefa, 1);
        Salvar();                         // grava a ordem final
    }

    // 3) Durante o arraste: reorganiza a lista ao vivo conforme a posição do mouse.
    private void Lista_DragOver(object sender, DragEventArgs e)
    {
        e.Handled = true;

        if (e.Data.GetData(typeof(Tarefa)) is not Tarefa arrastada)
        {
            e.Effects = DragDropEffects.None;   // não é uma tarefa: não aceita
            return;
        }
        e.Effects = DragDropEffects.Move;

        var posicao = e.GetPosition(ListaTarefas);

        // Conta quantas outras tarefas têm o "meio" acima do mouse.
        // Esse número é a posição onde a tarefa arrastada deve ficar.
        int novoIndice = 0;
        foreach (var t in tarefas)
        {
            if (t == arrastada) continue;
            if (ListaTarefas.ItemContainerGenerator.ContainerFromItem(t) is not FrameworkElement c) continue;

            double topo = c.TransformToAncestor(ListaTarefas).Transform(new Point(0, 0)).Y;
            if (posicao.Y > topo + c.ActualHeight / 2) novoIndice++;
        }

        int atual = tarefas.IndexOf(arrastada);
        if (atual >= 0 && novoIndice != atual)
        {
            tarefas.Move(atual, novoIndice);   // troca de lugar na lista
            ListaTarefas.UpdateLayout();       // atualiza a tela já, para a próxima conta usar posições novas
            DefinirOpacidade(arrastada, 0.45); // o "recipiente" pode ter sido recriado: reaplica o efeito
        }
    }

    // Muda a transparência do recipiente visual de uma tarefa.
    private void DefinirOpacidade(Tarefa t, double valor)
    {
        if (ListaTarefas.ItemContainerGenerator.ContainerFromItem(t) is UIElement c)
            c.Opacity = valor;
    }

    

    // OUTROS

    //usado pelas caixinhas de marcar e pela setinha: qualquer mudança salva.
    private void Chk_Click(object sender, RoutedEventArgs e) => Salvar();

    //botão de fechar 
    private void BtnFechar_Click(object sender, RoutedEventArgs e) => Close();

    //arrastar a janela segurando o cabeçalho.
    private void Cabecalho_MouseDown(object sender, MouseButtonEventArgs e) => DragMove();
}