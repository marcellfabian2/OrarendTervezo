using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace Orarend
{
    //osztály a teendőkhöz: szöveg és állapot
    public class TodoElem
    {
        public string Szoveg { get; set; }
        public bool Kesz { get; set; }
    }

    public partial class MainWindow : Window
    {
        private bool isDarkMode = false;
        //szótár az egyes tantárgyak maximális heti óraszámának meghatározására
        private Dictionary<string, int> limitKonyvtar = new Dictionary<string, int>
        {
            {"Matematika", 3}, {"Irodalom", 3}, {"Magyar nyelv", 1}, {"Történelem", 3},
            {"Angol nyelv", 3}, {"Német nyelv", 1}, {"Testnevelés", 3}, {"Osztályfőnöki", 1},
            {"Frontend", 3}, {"Backend", 3}, {"Adatbázis kezelés", 2}, {"Szoftvertesztelés", 2},
            {"Linux", 1}, {"Mobil alkalmazásfejlesztés", 2}, {"IKT_projektmunka", 3}, {"Szakmai angol nyelv", 1},
            {"Emelt Történelem", 2}, {"Emelt Matematika", 2}, {"Emelt Magyar", 2}, {"Emelt Angol", 2}, {"Emelt Digitális kultúra", 2}
        };

        //dátum alapú tároló a teendőkhöz --> az ObservableCollection --> a UI automatikus frissítése
        private Dictionary<DateTime, ObservableCollection<TodoElem>> naptarTarolo = new Dictionary<DateTime, ObservableCollection<TodoElem>>();
        private DateTime hetHetfoje; //az aktuálisan megjelenített hét hétfői napja
        private string csvPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "teendok.csv"); //mentési útvonal

        public MainWindow()
        {
            InitializeComponent();
            RacsGeneralas(); //órarend üres négyzeteinek létrehozása
            FrissitListakStilusa(); //tantárgylista ellenőrzése

            //aktuális hét hétfőjének kiszámítása a rendszeridő alapján
            DateTime ma = DateTime.Today;
            int napKulonbseg = (int)ma.DayOfWeek;
            if (napKulonbseg == 0) napKulonbseg = 7; //vasárnap korrekciója
            hetHetfoje = ma.AddDays(-(napKulonbseg - 1));

            BetoltTeendok(); //korábbi teendők betöltése fájlból
            FrissitNaptarNezet(); //naptár felület inicializálása
            FrissitMenuKijeloles(); //menüsor vizuális visszajelzése
            ApplyTheme(isDarkMode); //dark témát beállítja
        }

        //adatok beolvasása CSV fájlból és szétbontása dátumra, állapotra és szövegre
        private void BetoltTeendok()
        {
            if (!File.Exists(csvPath)) return;
            try
            {
                var sorok = File.ReadAllLines(csvPath, Encoding.UTF8);
                naptarTarolo.Clear(); // Tisztítás betöltés előtt
                foreach (var sor in sorok)
                {
                    var adatok = sor.Split(';');
                    if (adatok.Length == 3)
                    {
                        DateTime nap = DateTime.Parse(adatok[0]);
                        bool kesz = bool.Parse(adatok[1]);
                        string szoveg = adatok[2];

                        if (!naptarTarolo.ContainsKey(nap))
                            naptarTarolo[nap] = new ObservableCollection<TodoElem>();

                        naptarTarolo[nap].Add(new TodoElem { Szoveg = szoveg, Kesz = kesz });
                    }
                }
            }
            catch { }
        }

        //teendők elmentése --> végigjárja a szótárat és pontosvesszővel elválasztva fájlba írja
        private void MentTeendok()
        {
            try
            {
                List<string> sorok = new List<string>();
                foreach (var nap in naptarTarolo)
                {
                    foreach (var todo in nap.Value)
                    {
                        sorok.Add($"{nap.Key:yyyy-MM-dd};{todo.Kesz};{todo.Szoveg}");
                    }
                }
                var encodingNincsBOM = new UTF8Encoding(false);
                File.WriteAllLines(csvPath, sorok, encodingNincsBOM);
            }
            catch { }
        }

        //a naptár UI elemeinek (dátumok, listák) frissítése az aktuális hétnek megfelelően
        private void FrissitNaptarNezet()
        {
            HetFelirat.Text = hetHetfoje.ToString("yyyy. MMMM").ToUpper();
            for (int i = 0; i < 7; i++)
            {
                DateTime nap = hetHetfoje.AddDays(i);
                TextBlock dTxt = (TextBlock)this.FindName("Date" + i);
                if (dTxt != null) dTxt.Text = nap.ToString("MMM dd").ToUpper();

                if (!naptarTarolo.ContainsKey(nap)) naptarTarolo[nap] = new ObservableCollection<TodoElem>();
                ItemsControl ic = (ItemsControl)this.FindName("List" + i);
                if (ic != null)
                {
                    ic.ItemsSource = naptarTarolo[nap];
                    ic.ItemTemplate = GeneralTodoTemplate(nap); // Átadjuk a napot a törléshez
                }
            }
        }

        //dinamikus CheckBox és Törlés gomb létrehozása a teendőkhöz
        private DataTemplate GeneralTodoTemplate(DateTime aktualisNap)
        {
            DataTemplate dt = new DataTemplate();

            //StackPanel a checkbox és a törlés gomb vízszintes elrendezéséhez
            FrameworkElementFactory stackPanel = new FrameworkElementFactory(typeof(StackPanel));
            stackPanel.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

            //CheckBox a teendőhöz
            FrameworkElementFactory cb = new FrameworkElementFactory(typeof(CheckBox));
            cb.SetBinding(CheckBox.ContentProperty, new Binding("Szoveg"));
            cb.SetBinding(CheckBox.IsCheckedProperty, new Binding("Kesz") { Mode = BindingMode.TwoWay });
            cb.SetValue(CheckBox.VerticalAlignmentProperty, VerticalAlignment.Center);
            cb.AddHandler(CheckBox.ClickEvent, new RoutedEventHandler((s, e) => MentTeendok()));

            //törlés gomb (piros "X")
            FrameworkElementFactory btn = new FrameworkElementFactory(typeof(Button));
            btn.SetValue(Button.ContentProperty, "×");
            btn.SetValue(Button.ForegroundProperty, Brushes.Red);
            btn.SetValue(Button.BackgroundProperty, Brushes.Transparent);
            btn.SetValue(Button.BorderThicknessProperty, new Thickness(0));
            btn.SetValue(Button.FontWeightProperty, FontWeights.Bold);
            btn.SetValue(Button.MarginProperty, new Thickness(5, 0, 0, 0));
            btn.SetValue(Button.VerticalAlignmentProperty, VerticalAlignment.Center);

            //törlés esemény kezelése
            btn.AddHandler(Button.ClickEvent, new RoutedEventHandler((s, e) => {
                var elem = (s as Button).DataContext as TodoElem;
                if (elem != null && naptarTarolo.ContainsKey(aktualisNap))
                {
                    naptarTarolo[aktualisNap].Remove(elem);
                    MentTeendok(); //törlés után mentés a fájlba
                }
            }));

            stackPanel.AppendChild(cb);
            stackPanel.AppendChild(btn);
            dt.VisualTree = stackPanel;
            return dt;
        }

        //új teendő hozzáadása Enter billentyű lenyomásakor
        private void Todo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TextBox tb = sender as TextBox;
                //a Tag tulajdonság tárolja, hogy a hét hanyadik napjáról van szó
                DateTime celNap = hetHetfoje.AddDays(int.Parse(tb.Tag.ToString()));
                if (!string.IsNullOrWhiteSpace(tb.Text))
                {
                    if (!naptarTarolo.ContainsKey(celNap)) naptarTarolo[celNap] = new ObservableCollection<TodoElem>();
                    naptarTarolo[celNap].Add(new TodoElem { Szoveg = tb.Text, Kesz = false });
                    tb.Text = "";
                    MentTeendok();
                }
            }
        }

        //menüpontok félkövérré tétele az aktív panel alapján
        private void FrissitMenuKijeloles()
        {
            foreach (var item in ((Menu)((Grid)this.Content).Children[0]).Items)
            {
                if (item is MenuItem mi)
                {
                    if (OrarendPanel.Visibility == Visibility.Visible && mi.Header.ToString() == "Órarendtervező")
                        mi.FontWeight = FontWeights.Bold;
                    else if (TodoPanel.Visibility == Visibility.Visible && mi.Header.ToString() == "Teendők")
                        mi.FontWeight = FontWeights.Bold;
                    else
                        mi.FontWeight = FontWeights.Normal;
                }
            }
        }
        //hétváltó gombok
        private void ElozoHet_Click(object sender, RoutedEventArgs e) { hetHetfoje = hetHetfoje.AddDays(-7); FrissitNaptarNezet(); }
        private void KovetkezoHet_Click(object sender, RoutedEventArgs e) { hetHetfoje = hetHetfoje.AddDays(7); FrissitNaptarNezet(); }

        //navigáció az órarend és a teendők paneljei között
        private void OrarendNav_Click(object sender, RoutedEventArgs e)
        {
            OrarendPanel.Visibility = Visibility.Visible;
            TodoPanel.Visibility = Visibility.Collapsed;
            FrissitMenuKijeloles();
        }

        private void TeendokNav_Click(object sender, RoutedEventArgs e)
        {
            OrarendPanel.Visibility = Visibility.Collapsed;
            TodoPanel.Visibility = Visibility.Visible;
            FrissitMenuKijeloles();
        }

        //az órarend táblázat (Grid) vizuális generálása (idősávok és üres cellák)
        private void RacsGeneralas()
        {
            OrarendRacs.Children.Clear();
            string[] idopontok = { "1. óra\n7:30 - 8:15", "2. óra\n8:25 - 9:10", "3. óra\n9:20 - 10:05", "4. óra\n10:20 - 11:05", "5. óra\n11:15 - 12:00", "6. óra\n12:15 - 13:00", "7. óra\n13:10 - 13:55", "8. óra\n14:05 - 14:50", "9. óra\n14:55 - 15:40" };
            for (int i = 0; i < 9; i++) //órák száma
            {
                for (int j = 0; j < 6; j++) //napok száma + idősáv oszlop
                {
                    Border cella = new Border { BorderBrush = new SolidColorBrush(Color.FromRgb(230, 230, 230)), BorderThickness = new Thickness(0.5), Tag = i };
                    if (j == 0) //idősáv oszlop formázása
                    {
                        cella.Background = new SolidColorBrush(Color.FromRgb(248, 249, 250));
                        cella.Child = new TextBlock { Text = idopontok[i], VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, TextAlignment = TextAlignment.Center, FontSize = 10, Foreground = Brushes.DimGray };
                    }
                    else //interaktív órarendi cellák
                    {
                        cella.Background = Brushes.Transparent; cella.AllowDrop = true;
                        cella.DragOver += (s, ev) => { ev.Effects = DragDropEffects.Copy; ev.Handled = true; };
                        cella.Drop += Cella_Drop; //elem lerakása
                        cella.MouseDown += Cella_MouseDown; //elem megfogása vagy törlése
                    }
                    OrarendRacs.Children.Add(cella);
                }
            }
        }

        //ellenőrzi, hogy egy tantárgy elérte-e a heti limitet, és elszürkíti, ha igen
        private void FrissitListakStilusa()
        {
            var mindenLabel = OrarendRacs.Children.OfType<Border>().Select(b => b.Child as Label).Where(l => l != null).ToList();
            ListBox[] listak = { BalLista, EmeltLista, JobbLista };
            foreach (var lista in listak)
            {
                foreach (ListBoxItem item in lista.Items)
                {
                    string targyNev = item.Content.ToString();
                    int targyDb = mindenLabel.Count(l => l.Content.ToString() == targyNev);
                    bool tiltva = (limitKonyvtar.ContainsKey(targyNev) && targyDb >= limitKonyvtar[targyNev]) || mindenLabel.Count >= 35;
                    item.Opacity = tiltva ? 0.4 : 1.0; item.IsEnabled = !tiltva;
                }
            }
        }

        //a heti összóraszám kijelzése és a progress bar színezése (zöld --> narancs --> piros)
        private void FrissitProgress()
        {
            int db = OrarendRacs.Children.OfType<Border>().Count(b => b.Child is Label);
            HetiProgress.Value = db; OraSzamlaloSzoveg.Text = $"{db} / 35 óra";
            HetiProgress.Foreground = db >= 35 ? Brushes.Red : (db > 28 ? Brushes.Orange : new SolidColorBrush(Color.FromRgb(46, 204, 113)));
            FrissitListakStilusa();
        }

        //Drag & Drop indítása a listából az órarend felé
        private void List_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && sender is ListBox lb && lb.SelectedItem != null)
            {
                ListBoxItem item = lb.ItemContainerGenerator.ContainerFromItem(lb.SelectedItem) as ListBoxItem;
                if (item == null || !item.IsEnabled) return;
                string tipus = lb.Name == "BalLista" ? "Alap" : (lb.Name == "EmeltLista" ? "Emelt" : "Szakmai");
                //az adatokat karakterláncként adjuk át, "pipe" jellel (|) elválasztva
                DragDrop.DoDragDrop(lb, (lb.SelectedItem as ListBoxItem).Content.ToString() + "|" + tipus + "|LISTA", DragDropEffects.Copy);
            }
        }

        //órarendi cellára kattintás: bal gombbal áthelyezés (Drag), jobb gombbal törlés
        private void Cella_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Border cella = sender as Border;
            if (cella?.Child is Label targy)
            {
                if (e.RightButton == MouseButtonState.Pressed) { cella.Child = null; FrissitProgress(); }
                else if (e.LeftButton == MouseButtonState.Pressed)
                {
                    string info = targy.Content.ToString() + "|" + targy.Tag.ToString() + "|RACSOK";
                    UIElement mentes = cella.Child; cella.Child = null; // Ideiglenesen kivesszük
                    if (DragDrop.DoDragDrop(cella, info, DragDropEffects.Copy) == DragDropEffects.None) cella.Child = mentes; // Ha nem sikerült a drop, visszatesszük
                    FrissitProgress();
                }
            }
        }

        //elem leejtése egy cellára: szabályok ellenőrzése (emelt óra ideje, heti limit)
        private void Cella_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.StringFormat)) return;
            string[] adat = e.Data.GetData(DataFormats.StringFormat).ToString().Split('|');
            string targyNev = adat[0], targyTipus = adat[1], forras = adat[2];
            Border celCella = sender as Border;
            int oraIndex = (int)celCella.Tag;

            //szabály: emelt tárgy csak a nap végén (8-9. óra) lehet
            if (targyTipus == "Emelt" && oraIndex < 7)
            {
                MessageBox.Show("Az emelt szintű tárgyakat csak a 8. vagy 9. órában lehet felvenni!", "Időpont hiba", MessageBoxButton.OK, MessageBoxImage.Warning);
                e.Effects = DragDropEffects.None; return;
            }

            var mindenLabel = OrarendRacs.Children.OfType<Border>().Select(b => b.Child as Label).Where(l => l != null).ToList();

            //új elemnél ellenőrizzük a maximum korlátokat
            if (forras == "LISTA")
            {
                if (mindenLabel.Count >= 35 && celCella.Child == null) { e.Effects = DragDropEffects.None; return; }
                int jelenlegiDb = mindenLabel.Count(l => l.Content.ToString() == targyNev);
                if (limitKonyvtar.ContainsKey(targyNev) && jelenlegiDb >= limitKonyvtar[targyNev]) { e.Effects = DragDropEffects.None; return; }
            }

            //ha van ott valami, rákérdezünk a cserére
            if (celCella.Child is Label regi)
            {
                if (regi.Content.ToString() == targyNev) return;
                if (MessageBox.Show($"Biztosan lecseréled a(z) {regi.Content} órát erre: {targyNev}?", "Csere megerősítése", MessageBoxButton.YesNo) == MessageBoxResult.No)
                {
                    e.Effects = DragDropEffects.None; return;
                }
            }

            //új vizuális elem (Label) létrehozása és színezése típus szerint
            Label uj = new Label { Content = targyNev, Tag = targyTipus, Foreground = Brushes.White, FontWeight = FontWeights.Bold, HorizontalContentAlignment = HorizontalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center, Margin = new Thickness(2) };
            if (targyTipus == "Alap") uj.Background = new SolidColorBrush(Color.FromRgb(216, 27, 96));
            else if (targyTipus == "Emelt") uj.Background = new SolidColorBrush(Color.FromRgb(230, 126, 34));
            else uj.Background = new SolidColorBrush(Color.FromRgb(67, 160, 71));
            celCella.Child = uj; FrissitProgress();
        }

        //súgó ablak összeállítása a limitKonyvtar alapján
        private void Sugo_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("HASZNÁLATI ÚTMUTATÓ:");
            sb.AppendLine("-----------------------------------------");
            sb.AppendLine("- Tárgyak behúzása: Bal egérgomb.");
            sb.AppendLine("- Tárgy törlése: Jobb egérgomb a rácson.");
            sb.AppendLine("- Emelt tárgyak: Csak 8-9. óra.");
            sb.AppendLine("- Ha a heti terhelés (35 óra) betelt, több tárgy nem vehető fel.");
            sb.AppendLine("\nMAXIMÁLIS ÓRASZÁMOK:");
            foreach (var limit in limitKonyvtar)
                sb.AppendLine($"- {limit.Key}: {limit.Value} óra");
            MessageBox.Show(sb.ToString(), "Súgó", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        //teljes órarend törlése megerősítés után
        private void Torles_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Biztosan törölni szeretnéd az egész órarendet?", "Törlés", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                foreach (var border in OrarendRacs.Children.OfType<Border>()) if (border.Child is Label) border.Child = null;
                FrissitProgress();
            }
        }

        //képernyőkép
        private void Screenshot_Click(object sender, RoutedEventArgs e)
        {
            Orarend_resz.UpdateLayout();
            Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);

            var dpi = VisualTreeHelper.GetDpi(Orarend_resz);

            double width = Orarend_resz.ActualWidth;
            double height = Orarend_resz.ActualHeight;

            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                var vb = new VisualBrush(Orarend_resz);
                dc.DrawRectangle(vb, null, new Rect(new Point(0, 0), new Size(width, height)));
            }

            var rtb = new RenderTargetBitmap(
                (int)Math.Ceiling(width * dpi.DpiScaleX),
                (int)Math.Ceiling(height * dpi.DpiScaleY),
                dpi.PixelsPerInchX, dpi.PixelsPerInchY,
                PixelFormats.Pbgra32);

            rtb.Render(dv);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string filePath = System.IO.Path.Combine(desktopPath, $"Órarend_screenshot{DateTime.Now:yyyyMMdd_HHmmss}.png");

            using (var fs = new FileStream(filePath, FileMode.Create))
                encoder.Save(fs);

            MessageBox.Show("A képernyőkép elkészült, és az Asztal-ra mentve lett!");
        }

        //light-dark mode kódjai
        private void Beallitasok_Click(object sender, RoutedEventArgs e)
        {
            LightModeRadio.IsChecked = !isDarkMode;
            DarkModeRadio.IsChecked = isDarkMode;
            SettingsOverlay.Visibility = Visibility.Visible;
        }

        private void BeallitasMegse_Click(object sender, RoutedEventArgs e)
        {
            SettingsOverlay.Visibility = Visibility.Collapsed;
        }

        private void BeallitasMentes_Click(object sender, RoutedEventArgs e)
        {
            isDarkMode = DarkModeRadio.IsChecked == true;
            ApplyTheme(isDarkMode);
            SettingsOverlay.Visibility = Visibility.Collapsed;
        }

        private void ApplyTheme(bool dark)
        {
            if (dark)
            {
                Resources["AppBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(24, 26, 27));
                Resources["SurfaceBrush"] = new SolidColorBrush(Color.FromRgb(37, 40, 43));
                Resources["MenuBrush"] = new SolidColorBrush(Color.FromRgb(32, 34, 36));
                Resources["PrimaryTextBrush"] = new SolidColorBrush(Colors.White);
                Resources["SecondaryTextBrush"] = new SolidColorBrush(Color.FromRgb(180, 180, 180));
                Resources["BorderBrushTheme"] = new SolidColorBrush(Color.FromRgb(80, 80, 80));
                Resources["OverlayBrush"] = new SolidColorBrush(Color.FromArgb(170, 0, 0, 0));
            }
            else
            {
                Resources["AppBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(240, 242, 245));
                Resources["SurfaceBrush"] = new SolidColorBrush(Colors.White);
                Resources["MenuBrush"] = new SolidColorBrush(Colors.White);
                Resources["PrimaryTextBrush"] = new SolidColorBrush(Color.FromRgb(17, 17, 17));
                Resources["SecondaryTextBrush"] = new SolidColorBrush(Colors.Gray);
                Resources["BorderBrushTheme"] = new SolidColorBrush(Color.FromRgb(189, 195, 199));
                Resources["OverlayBrush"] = new SolidColorBrush(Color.FromArgb(102, 0, 0, 0));
            }
        }
    }
}