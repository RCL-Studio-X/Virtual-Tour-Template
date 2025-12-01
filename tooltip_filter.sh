#!/usr/bin/env bash
# EXACT functionality of the Python script using awk

file="$1"

# Ensure UTF-8 output (similar to Python TextIOWrapper settings)
export LC_ALL=C.UTF-8

awk '
BEGIN {
    # regex patterns equivalent to Python version
    tooltip = "^[[:space:]]*\\[Tooltip\\(\"([^\"]*)\"\\)\\][[:space:]]*$"
    header  = "^[[:space:]]*\\[Header\\(\"([^\"]*)\"\\)\\][[:space:]]*$"
}

{
    line = $0

    # Tooltip
    if (match(line, tooltip, m)) {
        text = m[1]
        print "/// <summary>" text "</summary>"
        print ""
        next
    }

    # Header (removed but line count preserved with 2 blank lines)
    if (match(line, header)) {
        print ""
        print ""
        next
    }

    # Default passthrough
    print line
}
' "$file"
