import sys
import re
import io

# --- Force stdout to UTF-8 and unbuffered (Windows/Doxygen safe) ---
# Replace sys.stdout with a UTF-8 writer regardless of console codepage.
sys.stdout = io.TextIOWrapper(
    sys.stdout.buffer,
    encoding='utf-8',
    errors='replace',  # prevents crashing on unencodable output
    line_buffering=True
)

# --- Patterns ---
tooltip = re.compile(r'^\s*\[Tooltip\("([^"]*)"\)\]\s*$')
header  = re.compile(r'^\s*\[Header\("([^"]*)"\)\]\s*$')

filepath = sys.argv[1]

with open(filepath, "r", encoding="utf-8") as f:
    for line in f:
        m_tooltip = tooltip.match(line)
        m_header = header.match(line)

        # Tooltip -> XML doc summary + blank line
        if m_tooltip:
            text = m_tooltip.group(1)
            print(f'/// <summary>{text}</summary>')
            print('')
            continue

        # Header -> removed but maintain line count
        if m_header:
            print('')
            print('')
            continue

        # Default passthrough (now safely UTF-8)
        print(line, end='')
