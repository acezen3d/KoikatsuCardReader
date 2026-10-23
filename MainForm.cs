using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using KoikatsuCardReader.Core;
using KoikatsuCardReader.Util;

namespace KoikatsuCardReader
{
    /// <summary>
    /// Main window. Layout lives in MainForm.Designer.cs. Read-only: never writes to card files.
    /// </summary>
    public partial class MainForm : Form
    {
        private CardScanResult _result;
        private CancellationTokenSource _cts;
        private Dictionary<string, ZipmodMapEntry> _zipmodMap = new Dictionary<string, ZipmodMapEntry>(StringComparer.OrdinalIgnoreCase);
        private bool _populating;

        public MainForm()
        {
            InitializeComponent();
            LoadZipmodMap();
        }

        private void LoadZipmodMap()
        {
            try
            {
                _zipmodMap = ZipmodMap.LoadLatest();
            }
            catch (Exception)
            {
                _zipmodMap = new Dictionary<string, ZipmodMapEntry>(StringComparer.OrdinalIgnoreCase);
            }

            UpdateZipmodMapLabel();
        }

        private void UpdateZipmodMapLabel()
        {
            labelZipmodMap.Text = _zipmodMap.Count == 0
                ? "Zipmod map: not built"
                : $"Zipmod map: {_zipmodMap.Count} entries";
            buttonOpenZipmodMap.Enabled = ZipmodMap.FindLatestFile() != null;
        }

        private void ButtonOpenZipmodMap_Click(object sender, EventArgs e)
        {
            var path = ZipmodMap.FindLatestFile();
            if (path == null) return;

            try
            {
                Process.Start(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Open zipmod map", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string FormatZipmod(string guid)
        {
            if (_zipmodMap.Count == 0)
                return $"{guid} -> (Missing)";

            return _zipmodMap.TryGetValue(guid, out var entry)
                ? $"{guid} -> {entry.FileName}"
                : $"{guid} -> (Missing)";
        }

        private void DataGridView_SelectionChanged(object sender, EventArgs e)
        {
            if (_populating) return;
            UpdateDetailsView();
        }

        private void ViewMode_CheckedChanged(object sender, EventArgs e)
        {
            UpdateDetailsView();
        }

        private void ButtonBrowse_Click(object sender, EventArgs e)
        {
            var selected = FolderPickerDialog.Show("Select the folder to scan", textBoxPath.Text.Trim(), Handle);

            textBoxPath.Text = selected ?? string.Empty;

            // Cancelled: clear the folder and everything that was scanned from it.
            if (selected == null)
            {
                ClearResults();
                labelStatus.Text = "Ready";
            }
        }

        private async void ButtonBuildZipmodMap_Click(object sender, EventArgs e)
        {
            var folder = FolderPickerDialog.Show("Select the folder containing zipmods", string.Empty, Handle);
            if (folder == null) return;

            SetScanningState(true);
            buttonCancel.Enabled = false;
            labelStatus.Text = "Building zipmod map";

            try
            {
                var map = await Task.Run(() => ZipmodMap.Build(folder));
                var mapPath = ZipmodMap.Save(map);

                _zipmodMap = map;
                UpdateZipmodMapLabel();
                UpdateDetailsView();
                labelStatus.Text = $"Zipmod map saved: {Path.GetFileName(mapPath)} ({map.Count} entries)";

                if (map.Count == 0)
                {
                    MessageBox.Show(this, "No zipmods were found in the selected folder", "Build zipmod map",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                labelStatus.Text = "Failed to build zipmod map";
                MessageBox.Show(this, ex.Message, "Build zipmod map", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetScanningState(false);
            }
        }

        private async void ButtonScan_Click(object sender, EventArgs e)
        {
            var path = textBoxPath.Text.Trim();
            if (!Directory.Exists(path))
            {
                MessageBox.Show(this, $"The directory does not exist:\n{path}", "Scan",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            ClearResults();
            SetScanningState(true);
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            var recursive = checkBoxRecursive.Checked;
            var parseEmbeddedCharacter = checkBoxParseCharactersInScenes.Checked;

            var progressReporter = new Progress<(int Done, int Total)>(p =>
            {
                progressBar.Maximum = Math.Max(1, p.Total);
                progressBar.Value = Math.Min(p.Done, progressBar.Maximum);
                labelStatus.Text = $"Scanning {p.Done} / {p.Total} images";
            });

            try
            {
                labelStatus.Text = "Scanning";
                var scanner = new CardScanner();
                var result = await Task.Run(() => scanner.Scan(path, recursive, parseEmbeddedCharacter, progressReporter, token));

                if (result == null || token.IsCancellationRequested)
                {
                    ClearResults();
                    labelStatus.Text = "Scan cancelled and results discarded";
                    return;
                }

                _result = result;
                PopulateGrid();

                var skipped = result.SkippedDirectories.Count;
                var status = $"Done: {result.CardCount} card(s), {result.NonCardCount} non-card image(s)";
                if (skipped > 0)
                    status += $", {skipped} directory(s) skipped (no access)";

                labelStatus.Text = status;
                buttonExport.Enabled = result.CardCount > 0;
            }
            catch (Exception ex)
            {
                ClearResults();
                labelStatus.Text = "Scan failed";
                MessageBox.Show(this, ex.Message, "Scan failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetScanningState(false);
                _cts.Dispose();
                _cts = null;
            }
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            if (_cts == null) return;
            labelStatus.Text = "Cancelling";
            buttonCancel.Enabled = false;
            _cts.Cancel();
        }

        private void ButtonExport_Click(object sender, EventArgs e)
        {
            if (_result == null || _result.Cards.Count == 0)
            {
                MessageBox.Show(this, "There are no results to export", "Export JSON",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            List<string> plugins;
            List<string> zipmods;
            string defaultFileName;

            if (radioButtonAll.Checked)
            {
                defaultFileName = "All results.json";
                plugins = _result.Cards
                    .SelectMany(card => card.Plugins)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(guid => guid, StringComparer.Ordinal)
                    .ToList();
                zipmods = _result.Cards
                    .SelectMany(card => card.Zipmods)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(guid => guid, StringComparer.Ordinal)
                    .Select(FormatZipmod)
                    .ToList();
            }
            else
            {
                if (dataGridView.SelectedRows.Count == 0
                    || dataGridView.SelectedRows[0].Tag is not CardInfo card)
                {
                    MessageBox.Show(this, "Select a card first", "Export JSON",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                defaultFileName = $"{card.FileName}.json";
                plugins = card.Plugins;
                zipmods = card.Zipmods.Select(FormatZipmod).ToList();
            }

            if (plugins.Count == 0 && zipmods.Count == 0)
            {
                MessageBox.Show(this, "There are no plugins or zipmods to export", "Export JSON",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                DefaultExt = "json",
                FileName = defaultFileName,
            })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    JsonExporter.Save(dialog.FileName, new { Plugins = plugins, Zipmods = zipmods });
                    labelStatus.Text = $"Exported to {dialog.FileName}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Export failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void SetScanningState(bool scanning)
        {
            buttonScan.Enabled = !scanning;
            buttonCancel.Enabled = scanning;
            buttonBuildZipmodMap.Enabled = !scanning;
            buttonBrowse.Enabled = !scanning;
            textBoxPath.Enabled = !scanning;
            checkBoxRecursive.Enabled = !scanning;
            checkBoxParseCharactersInScenes.Enabled = !scanning;
            buttonExport.Enabled = !scanning && _result != null && _result.Cards.Count > 0;
            if (!scanning) progressBar.Value = 0;
        }

        private void ClearResults()
        {
            _result = null;
            dataGridView.Rows.Clear();
            treeViewDetails.Nodes.Clear();
            buttonExport.Enabled = false;
            progressBar.Value = 0;
        }

        private void PopulateGrid()
        {
            _populating = true;
            dataGridView.SuspendLayout();
            try
            {
                dataGridView.Rows.Clear();

                foreach (var card in _result.Cards)
                {
                    var status = card.Error != null ? "Error" : "OK";
                    var name = card.CharacterInfo ?? card.CoordinateName;
                    if (string.IsNullOrEmpty(name)) name = "(Empty)";

                    var index = dataGridView.Rows.Add(
                        card.FileName,
                        card.FilePath,
                        name,
                        card.Game ?? "(Empty)",
                        card.Type,
                        card.Plugins.Count,
                        card.Zipmods.Count,
                        status);

                    var row = dataGridView.Rows[index];
                    row.Tag = card;

                    if (card.Error != null)
                    {
                        row.DefaultCellStyle.ForeColor = Color.Firebrick;
                    }
                    else
                    {
                        row.DefaultCellStyle.BackColor = GetRowColor(card.Type);
                    }
                }
            }
            finally
            {
                dataGridView.ResumeLayout();
                _populating = false;
            }

            if (dataGridView.Rows.Count > 0)
                dataGridView.Rows[0].Selected = true;

            UpdateDetailsView();
        }

        private static Color GetRowColor(string type)
        {
            switch (type)
            {
                case CardMarker.Character:
                    return Color.FromArgb(232, 247, 232);
                case CardMarker.Coordinate:
                    return Color.FromArgb(255, 248, 220);
                case CardMarker.Scene:
                    return Color.FromArgb(230, 242, 255);
                default:
                    return SystemColors.Window;
            }
        }

        private void UpdateDetailsView()
        {
            treeViewDetails.BeginUpdate();
            try
            {
                treeViewDetails.Nodes.Clear();

                if (_result == null) return;

                if (radioButtonAll.Checked)
                {
                    var allPluginIds = _result.Cards
                        .SelectMany(card => card.Plugins)
                        .Distinct(StringComparer.Ordinal)
                        .OrderBy(guid => guid, StringComparer.Ordinal)
                        .ToList();
                    var allZipmodIds = _result.Cards
                        .SelectMany(card => card.Zipmods)
                        .Distinct(StringComparer.Ordinal)
                        .OrderBy(guid => guid, StringComparer.Ordinal)
                        .ToList();

                    var allPlugins = treeViewDetails.Nodes.Add($"Plugins ({allPluginIds.Count})");
                    foreach (var guid in allPluginIds)
                        allPlugins.Nodes.Add(guid);

                    var allZipmods = treeViewDetails.Nodes.Add($"Zipmods ({allZipmodIds.Count})");
                    foreach (var guid in allZipmodIds)
                        allZipmods.Nodes.Add(FormatZipmod(guid));

                    allPlugins.Expand();
                    allZipmods.Expand();
                    return;
                }

                if (dataGridView.SelectedRows.Count == 0) return;
                if (dataGridView.SelectedRows[0].Tag is not CardInfo card) return;

                var plugins = treeViewDetails.Nodes.Add($"Plugins ({card.Plugins.Count})");
                foreach (var guid in card.Plugins)
                    plugins.Nodes.Add(guid);

                var zipmods = treeViewDetails.Nodes.Add($"Zipmods ({card.Zipmods.Count})");
                foreach (var guid in card.Zipmods)
                    zipmods.Nodes.Add(FormatZipmod(guid));

                plugins.Expand();
                zipmods.Expand();
            }
            finally
            {
                treeViewDetails.EndUpdate();
            }
        }

    }
}
