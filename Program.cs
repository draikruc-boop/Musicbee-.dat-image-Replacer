using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace DATImageReplacer
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    internal sealed class MainForm : Form
    {
        private readonly List<string> datPaths = new List<string>();
        private readonly Label datLabel = new Label();
        private readonly Label imageLabel = new Label();
        private readonly Label infoLabel = new Label();
        private readonly PictureBox originalPreview = CreatePreview();
        private readonly PictureBox replacementPreview = CreatePreview();
        private readonly Button replaceButton = new Button();
        private Image sourceImage;
        private string sourcePath;
        private Size currentSize;

        internal MainForm()
        {
            Text = "DAT Image Replacer";
            Width = 850;
            Height = 590;
            MinimumSize = new Size(760, 540);
            StartPosition = FormStartPosition.CenterScreen;
            BuildInterface();
            FormClosed += delegate { DisposePreviews(); };
        }

        private void BuildInterface()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(14),
                ColumnCount = 1,
                RowCount = 7
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(layout);

            layout.Controls.Add(new Label
            {
                Text = "DAT Image Replacer",
                AutoSize = true,
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 2)
            }, 0, 0);
            layout.Controls.Add(new Label
            {
                Text = "Replace JPEG image data stored in .dat files with another picture.",
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 12)
            }, 0, 1);

            var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = false, Margin = new Padding(0, 0, 0, 6) };
            var openDatButton = new Button { Text = "Open .DAT file(s)...", AutoSize = true };
            openDatButton.Click += ChooseDatFiles;
            var chooseImageButton = new Button { Text = "Choose replacement image...", AutoSize = true, Margin = new Padding(8, 3, 3, 3) };
            chooseImageButton.Click += ChooseReplacementImage;
            buttons.Controls.Add(openDatButton);
            buttons.Controls.Add(chooseImageButton);
            layout.Controls.Add(buttons, 0, 2);

            var labels = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 8) };
            datLabel.AutoSize = true;
            datLabel.ForeColor = Color.FromArgb(85, 85, 85);
            datLabel.Text = "No DAT files selected";
            imageLabel.AutoSize = true;
            imageLabel.ForeColor = Color.FromArgb(85, 85, 85);
            imageLabel.Text = "No replacement image selected";
            labels.Controls.Add(datLabel);
            labels.Controls.Add(imageLabel);
            layout.Controls.Add(labels, 0, 3);

            var previews = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Margin = new Padding(0) };
            previews.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            previews.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            previews.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            previews.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            previews.Controls.Add(new Label { Text = "Original DAT image", AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold), Margin = new Padding(0, 0, 8, 5) }, 0, 0);
            previews.Controls.Add(new Label { Text = "Replacement preview", AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold), Margin = new Padding(8, 0, 0, 5) }, 1, 0);
            previews.Controls.Add(originalPreview, 0, 1);
            previews.Controls.Add(replacementPreview, 1, 1);
            originalPreview.Margin = new Padding(0, 0, 8, 0);
            replacementPreview.Margin = new Padding(8, 0, 0, 0);
            originalPreview.Image = Placeholder("Open a .dat file to preview it");
            replacementPreview.Image = Placeholder("Choose an image to preview it");
            layout.Controls.Add(previews, 0, 4);

            infoLabel.AutoSize = true;
            infoLabel.Text = " ";
            infoLabel.Margin = new Padding(0, 10, 0, 4);
            layout.Controls.Add(infoLabel, 0, 5);

            var bottom = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoSize = true, Margin = new Padding(0, 8, 0, 0) };
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bottom.Controls.Add(new Label { Text = "Files are replaced directly (no backup is created).", AutoSize = true, ForeColor = Color.FromArgb(85, 85, 85), Anchor = AnchorStyles.Left }, 0, 0);
            replaceButton.Text = "Replace image in selected DAT file(s)";
            replaceButton.AutoSize = true;
            replaceButton.Enabled = false;
            replaceButton.Click += ReplaceImages;
            bottom.Controls.Add(replaceButton, 1, 0);
            layout.Controls.Add(bottom, 0, 6);
        }

        private static PictureBox CreatePreview()
        {
            return new PictureBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.WhiteSmoke, SizeMode = PictureBoxSizeMode.Zoom };
        }

        private static Bitmap Placeholder(string text)
        {
            var bitmap = new Bitmap(400, 280);
            using (var graphics = Graphics.FromImage(bitmap))
            using (var font = new Font("Segoe UI", 10))
            using (var brush = new SolidBrush(Color.FromArgb(95, 95, 95)))
            {
                graphics.Clear(Color.WhiteSmoke);
                var bounds = graphics.MeasureString(text, font, 360);
                graphics.DrawString(text, font, brush, (bitmap.Width - bounds.Width) / 2, (bitmap.Height - bounds.Height) / 2);
            }
            return bitmap;
        }

        private void ChooseDatFiles(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog { Title = "Select DAT image file(s)", Filter = "DAT files (*.dat)|*.dat|All files (*.*)|*.*", Multiselect = true })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                var validPaths = new List<string>();
                var failures = new List<string>();
                Image firstImage = null;
                foreach (var path in dialog.FileNames)
                {
                    try
                    {
                        using (var image = LoadImage(path))
                        {
                            if (firstImage == null)
                                firstImage = new Bitmap(image);
                        }
                        validPaths.Add(path);
                    }
                    catch (Exception exception)
                    {
                        failures.Add(Path.GetFileName(path) + ": " + exception.Message);
                    }
                }

                if (validPaths.Count == 0)
                {
                    if (firstImage != null)
                        firstImage.Dispose();
                    MessageBox.Show(this, "None of the selected files could be opened as images.", "DAT Image Replacer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                datPaths.Clear();
                datPaths.AddRange(validPaths);
                currentSize = firstImage.Size;
                SetPreview(originalPreview, firstImage);
                datLabel.Text = validPaths.Count + " DAT file(s): " + string.Join(", ", validPaths.Take(3).Select(Path.GetFileName)) + (validPaths.Count > 3 ? " ..." : string.Empty);
                UpdateReplacementPreview();
                UpdateInfo();
                UpdateButton();

                if (failures.Count > 0)
                    MessageBox.Show(this, "These files were skipped because they are not readable images:\n\n" + string.Join("\n", failures.Take(8)), "DAT Image Replacer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ChooseReplacementImage(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog
            {
                Title = "Choose replacement image",
                Filter = "Image files (*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tif;*.tiff)|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tif;*.tiff|All files (*.*)|*.*"
            })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                try
                {
                    var newImage = LoadImage(dialog.FileName);
                    if (sourceImage != null)
                        sourceImage.Dispose();
                    sourceImage = newImage;
                    sourcePath = dialog.FileName;
                    imageLabel.Text = string.Format("Replacement: {0} - {1} x {2}", Path.GetFileName(sourcePath), sourceImage.Width, sourceImage.Height);
                    UpdateReplacementPreview();
                    UpdateButton();
                }
                catch (Exception exception)
                {
                    MessageBox.Show(this, "Could not open that image:\n" + exception.Message, "DAT Image Replacer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void UpdateReplacementPreview()
        {
            if (sourceImage != null)
                SetPreview(replacementPreview, currentSize.IsEmpty ? new Bitmap(sourceImage) : MakeReplacement(sourceImage, currentSize));
        }

        private void UpdateInfo()
        {
            if (datPaths.Count == 0)
            {
                infoLabel.Text = " ";
                return;
            }

            var sizes = new SortedSet<string>();
            foreach (var path in datPaths)
            {
                try
                {
                    using (var image = LoadImage(path))
                        sizes.Add(image.Width + "x" + image.Height);
                }
                catch
                {
                }
            }
            infoLabel.Text = "Selected DAT image size(s): " + (sizes.Count == 0 ? "unknown" : string.Join(", ", sizes)) + ". Each file keeps its own dimensions.";
        }

        private void UpdateButton()
        {
            replaceButton.Enabled = datPaths.Count > 0 && sourceImage != null;
        }

        private void ReplaceImages(object sender, EventArgs e)
        {
            var names = string.Join("\n", datPaths.Take(8).Select(Path.GetFileName));
            if (datPaths.Count > 8)
                names += "\n...and " + (datPaths.Count - 8) + " more";
            var answer = MessageBox.Show(this,
                "Replace the image in " + datPaths.Count + " DAT file(s)?\n\n" + names + "\n\nThis permanently overwrites the selected files. No backups are created.",
                "DAT Image Replacer", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (answer != DialogResult.Yes)
                return;

            var succeeded = new List<string>();
            var failures = new List<string>();
            foreach (var path in datPaths)
            {
                var tempPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    Size targetSize;
                    using (var image = LoadImage(path))
                        targetSize = image.Size;
                    using (var replacement = MakeReplacement(sourceImage, targetSize))
                        File.WriteAllBytes(tempPath, EncodeJpeg(replacement));
                    File.Replace(tempPath, path, null);
                    succeeded.Add(path);
                }
                catch (Exception exception)
                {
                    try
                    {
                        if (File.Exists(tempPath))
                            File.Delete(tempPath);
                    }
                    catch
                    {
                    }
                    failures.Add(Path.GetFileName(path) + ": " + exception.Message);
                }
            }

            var message = succeeded.Count > 0 ? "Successfully replaced " + succeeded.Count + " file(s).\nNo backup files were created." : "No files were replaced.";
            if (failures.Count > 0)
                message += "\n\nErrors:\n" + string.Join("\n", failures.Take(10));
            MessageBox.Show(this, message, "DAT Image Replacer", MessageBoxButtons.OK, failures.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);

            if (succeeded.Count > 0)
            {
                try
                {
                    SetPreview(originalPreview, LoadImage(succeeded[0]));
                    UpdateInfo();
                }
                catch
                {
                }
            }
        }

        private static Image LoadImage(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var image = Image.FromStream(stream, false, true))
                return new Bitmap(image);
        }

        private static Bitmap MakeReplacement(Image source, Size targetSize)
        {
            var output = new Bitmap(targetSize.Width, targetSize.Height, PixelFormat.Format24bppRgb);
            using (var graphics = Graphics.FromImage(output))
            {
                graphics.Clear(Color.Black);
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                var scale = Math.Max((double)targetSize.Width / source.Width, (double)targetSize.Height / source.Height);
                var width = (float)(source.Width * scale);
                var height = (float)(source.Height * scale);
                var destination = new RectangleF((targetSize.Width - width) / 2, (targetSize.Height - height) / 2, width, height);
                graphics.DrawImage(source, destination);
            }
            return output;
        }

        private static byte[] EncodeJpeg(Image image)
        {
            var codec = ImageCodecInfo.GetImageEncoders().First(item => item.MimeType == "image/jpeg");
            using (var parameters = new EncoderParameters(1))
            using (var quality = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 95L))
            using (var stream = new MemoryStream())
            {
                parameters.Param[0] = quality;
                image.Save(stream, codec, parameters);
                return stream.ToArray();
            }
        }

        private static void SetPreview(PictureBox preview, Image image)
        {
            var previous = preview.Image;
            preview.Image = image;
            if (previous != null)
                previous.Dispose();
        }

        private void DisposePreviews()
        {
            if (sourceImage != null)
                sourceImage.Dispose();
            if (originalPreview.Image != null)
                originalPreview.Image.Dispose();
            if (replacementPreview.Image != null)
                replacementPreview.Image.Dispose();
        }
    }
}