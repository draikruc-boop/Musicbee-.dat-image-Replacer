import io
import os
import tkinter as tk
from tkinter import filedialog, messagebox, ttk

from PIL import Image, ImageOps, ImageTk, UnidentifiedImageError

APP_TITLE = "DAT Image Replacer"
PREVIEW_SIZE = (360, 270)
JPEG_QUALITY = 95


def open_image(path):
    """Open image data even when the file has a .dat extension."""
    with Image.open(path) as im:
        im.load()
        return im.copy()


def make_replacement(source, target_size):
    """Fill the target canvas while preserving aspect ratio (center-crop if needed)."""
    image = source.convert("RGB")
    return ImageOps.fit(
        image,
        target_size,
        method=Image.Resampling.LANCZOS,
        centering=(0.5, 0.5),
    )


class DATImageReplacer:
    def __init__(self, root):
        self.root = root
        self.root.title(APP_TITLE)
        self.root.geometry("850x590")
        self.root.minsize(760, 540)

        self.dat_paths = []
        self.source_path = None
        self.source_image = None
        self.current_preview = None
        self.new_preview = None
        self.current_size = None

        self._build_ui()

    def _build_ui(self):
        outer = ttk.Frame(self.root, padding=14)
        outer.pack(fill="both", expand=True)

        heading = ttk.Label(
            outer,
            text="DAT Image Replacer",
            font=("Segoe UI", 18, "bold"),
        )
        heading.pack(anchor="w")
        ttk.Label(
            outer,
            text="Replace JPEG image data stored in .dat files with another picture.",
        ).pack(anchor="w", pady=(2, 12))

        file_row = ttk.Frame(outer)
        file_row.pack(fill="x", pady=(0, 8))
        ttk.Button(file_row, text="Open .DAT file(s)…", command=self.choose_dat).pack(side="left")
        ttk.Button(file_row, text="Choose replacement image…", command=self.choose_image).pack(side="left", padx=8)

        self.dat_label = ttk.Label(outer, text="No DAT files selected", foreground="#555")
        self.dat_label.pack(anchor="w", pady=(0, 4))
        self.image_label = ttk.Label(outer, text="No replacement image selected", foreground="#555")
        self.image_label.pack(anchor="w", pady=(0, 10))

        previews = ttk.Frame(outer)
        previews.pack(fill="both", expand=True)
        previews.columnconfigure(0, weight=1)
        previews.columnconfigure(1, weight=1)
        previews.rowconfigure(1, weight=1)

        ttk.Label(previews, text="Original DAT image", font=("Segoe UI", 10, "bold")).grid(
            row=0, column=0, sticky="w", padx=(0, 8), pady=(0, 5)
        )
        ttk.Label(previews, text="Replacement preview", font=("Segoe UI", 10, "bold")).grid(
            row=0, column=1, sticky="w", padx=(8, 0), pady=(0, 5)
        )

        self.original_box = ttk.Label(
            previews, text="Open a .dat file to preview it", anchor="center",
            relief="groove"
        )
        self.original_box.grid(row=1, column=0, sticky="nsew", padx=(0, 8))
        self.replacement_box = ttk.Label(
            previews, text="Choose an image to preview it", anchor="center",
            relief="groove"
        )
        self.replacement_box.grid(row=1, column=1, sticky="nsew", padx=(8, 0))

        self.info_label = ttk.Label(outer, text=" ")
        self.info_label.pack(anchor="w", pady=(10, 4))

        bottom = ttk.Frame(outer)
        bottom.pack(fill="x", pady=(8, 0))
        ttk.Label(
            bottom,
            text="Files are replaced directly (no backup is created).",
            foreground="#555",
        ).pack(side="left")
        self.replace_button = ttk.Button(
            bottom, text="Replace image in selected DAT file(s)",
            command=self.replace_images, state="disabled"
        )
        self.replace_button.pack(side="right")

        ttk.Label(
            outer,
            text="The replacement is center-cropped to each DAT image's original dimensions and saved as JPEG data.",
            foreground="#555",
            wraplength=800,
        ).pack(anchor="w", pady=(10, 0))

    def choose_dat(self):
        paths = filedialog.askopenfilenames(
            title="Select DAT image file(s)",
            filetypes=[("DAT files", "*.dat"), ("All files", "*.*")]
        )
        if not paths:
            return
        valid = []
        first_image = None
        errors = []
        for path in paths:
            try:
                im = open_image(path)
                if first_image is None:
                    first_image = im
                valid.append(path)
            except Exception as exc:
                errors.append(f"{os.path.basename(path)}: {exc}")
        if not valid:
            messagebox.showerror(APP_TITLE, "None of the selected files could be opened as images.")
            return
        self.dat_paths = valid
        self.dat_label.config(
            text=f"{len(valid)} DAT file(s): " + ", ".join(os.path.basename(p) for p in valid[:3])
            + (" …" if len(valid) > 3 else "")
        )
        self.current_size = first_image.size
        self._show_preview(first_image, self.original_box, "current")
        self._update_replacement_preview()
        self._update_info()
        self._update_button()
        if errors:
            messagebox.showwarning(
                APP_TITLE,
                "These files were skipped because they are not readable images:\n\n" + "\n".join(errors[:8])
            )

    def choose_image(self):
        path = filedialog.askopenfilename(
            title="Choose replacement image",
            filetypes=[
                ("Image files", "*.jpg *.jpeg *.png *.bmp *.gif *.tif *.tiff *.webp"),
                ("All files", "*.*"),
            ],
        )
        if not path:
            return
        try:
            self.source_image = open_image(path)
            self.source_path = path
        except (UnidentifiedImageError, OSError) as exc:
            messagebox.showerror(APP_TITLE, f"Could not open that image:\n{exc}")
            return
        self.image_label.config(
            text=f"Replacement: {os.path.basename(path)} — {self.source_image.width} × {self.source_image.height}"
        )
        self._update_replacement_preview()
        self._update_info()
        self._update_button()

    def _show_preview(self, image, widget, which):
        preview = image.copy()
        preview.thumbnail(PREVIEW_SIZE, Image.Resampling.LANCZOS)
        photo = ImageTk.PhotoImage(preview)
        widget.config(image=photo, text="")
        if which == "current":
            self.current_preview = photo
        else:
            self.new_preview = photo

    def _update_replacement_preview(self):
        if self.source_image is None:
            return
        if self.current_size:
            preview_image = make_replacement(self.source_image, self.current_size)
        else:
            preview_image = self.source_image.copy().convert("RGB")
        self._show_preview(preview_image, self.replacement_box, "new")

    def _update_info(self):
        if not self.dat_paths:
            self.info_label.config(text=" ")
            return
        sizes = []
        for path in self.dat_paths:
            try:
                with Image.open(path) as im:
                    sizes.append(f"{im.width}×{im.height}")
            except Exception:
                pass
        unique = sorted(set(sizes))
        size_text = ", ".join(unique) if unique else "unknown"
        self.info_label.config(
            text=f"Selected DAT image size(s): {size_text}. Each file keeps its own dimensions."
        )

    def _update_button(self):
        state = "normal" if self.dat_paths and self.source_image is not None else "disabled"
        self.replace_button.config(state=state)

    def replace_images(self):
        if not self.dat_paths or self.source_image is None:
            return
        names = "\n".join(os.path.basename(p) for p in self.dat_paths[:8])
        if len(self.dat_paths) > 8:
            names += f"\n…and {len(self.dat_paths) - 8} more"
        answer = messagebox.askyesno(
            APP_TITLE,
            f"Replace the image in {len(self.dat_paths)} DAT file(s)?\n\n{names}\n\n"
            "A backup will be created first if the backup option is checked."
        )
        if not answer:
            return

        succeeded = []
        failures = []
        for path in self.dat_paths:
            try:
                with Image.open(path) as im:
                    target_size = im.size
                replacement = make_replacement(self.source_image, target_size)

                # No backup: replace the original DAT file directly.
                buffer = io.BytesIO()
                replacement.save(buffer, format="JPEG", quality=JPEG_QUALITY, optimize=True)
                data = buffer.getvalue()

                # Write to a temporary file first, then atomically replace the DAT file.
                temp_path = path + ".tmp"
                with open(temp_path, "wb") as f:
                    f.write(data)
                os.replace(temp_path, path)
                succeeded.append(path)
            except Exception as exc:
                failures.append(f"{os.path.basename(path)}: {exc}")

        if succeeded:
            message = f"Successfully replaced {len(succeeded)} file(s)."
            message += "\nNo backup files were created."
        else:
            message = "No files were replaced."
        if failures:
            message += "\n\nErrors:\n" + "\n".join(failures[:10])
        if failures:
            messagebox.showwarning(APP_TITLE, message)
        else:
            messagebox.showinfo(APP_TITLE, message)

        if succeeded:
            try:
                self._show_preview(open_image(succeeded[0]), self.original_box, "current")
            except Exception:
                pass


def main():
    root = tk.Tk()
    try:
        style = ttk.Style(root)
        if "vista" in style.theme_names():
            style.theme_use("vista")
    except Exception:
        pass
    DATImageReplacer(root)
    root.mainloop()


if __name__ == "__main__":
    main()
