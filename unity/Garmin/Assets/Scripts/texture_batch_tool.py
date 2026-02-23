import os
import csv
import argparse
from PIL import Image
from PIL import UnidentifiedImageError

def bytes_to_mb(bytes_size):
    """Converts bytes to megabytes."""
    return bytes_size / (1024 * 1024)

def get_image_resolution(filepath):
    """
    Attempts to get the resolution of an image file using Pillow.
    Returns "N/A" if resolution cannot be determined or if the file is not an image.
    """
    try:
        with Image.open(filepath) as img:
            return f"{img.width}x{img.height}"
    except UnidentifiedImageError:
        return "Not an image / Corrupt"
    except FileNotFoundError:
        return "File not found"
    except Exception as e:
        return f"Error reading image: {e}"

def audit_textures(directory_path, output_csv_path, size_threshold_mb):
    """
    Walks a directory tree, finds .png and .tga files larger than a threshold,
    and outputs a CSV report with filename, size, and resolution.
    """
    report_data = []
    # Add CSV header
    report_data.append(['Filename', 'Path', 'Size (MB)', 'Resolution'])

    # Convert MB threshold to bytes for comparison
    size_threshold_bytes = size_threshold_mb * 1024 * 1024

    print(f"Starting texture audit in: {directory_path}")
    print(f"Looking for .png and .tga files larger than {size_threshold_mb} MB...")

    for root, _, files in os.walk(directory_path):
        for filename in files:
            # Check for file extension (case-insensitive)
            if filename.lower().endswith(('.png', '.tga')):
                filepath = os.path.join(root, filename)
                try:
                    file_size = os.path.getsize(filepath)
                    if file_size > size_threshold_bytes:
                        resolution = get_image_resolution(filepath)
                        report_data.append([
                            filename,
                            os.path.relpath(filepath, directory_path), # Relative path for clarity
                            f"{bytes_to_mb(file_size):.2f}",
                            resolution
                        ])
                        print(f"Found large texture: {filename} ({bytes_to_mb(file_size):.2f} MB, {resolution})")
                except FileNotFoundError:
                    print(f"Warning: File not found during audit: {filepath}")
                except PermissionError:
                    print(f"Warning: Permission denied for file: {filepath}")
                except Exception as e:
                    print(f"Error processing {filepath}: {e}")

    # Write the report to CSV
    try:
        with open(output_csv_path, 'w', newline='', encoding='utf-8') as csvfile:
            csv_writer = csv.writer(csvfile)
            csv_writer.writerows(report_data)
        print(f"\nAudit complete. Report saved to: {output_csv_path}")
        print(f"Total large textures found: {len(report_data) - 1}") # Subtract header row
    except IOError as e:
        print(f"Error writing CSV file: {e}")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(
        description="Audits a directory for large .png and .tga texture files.",
        formatter_class=argparse.RawTextHelpFormatter
    )
    parser.add_argument(
        "directory",
        help="The root directory to start the audit from."
    )
    parser.add_argument(
        "-o", "--output",
        default="texture_audit_report.csv",
        help="Path for the output CSV report. (default: texture_audit_report.csv)"
    )
    parser.add_argument(
        "-s", "--size-threshold",
        type=float,
        default=2.0,
        help="Minimum file size in MB for a texture to be reported. (default: 2.0 MB)"
    )

    args = parser.parse_args()

    # Verify directory exists
    if not os.path.isdir(args.directory):
        print(f"Error: Directory not found: {args.directory}")
        exit(1)

    # Ensure Pillow is installed
    try:
        from PIL import Image
    except ImportError:
        print("Error: Pillow library not found.")
        print("Please install it using: pip install Pillow")
        exit(1)

    audit_textures(args.directory, args.output, args.size_threshold)