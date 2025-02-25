#!/bin/bash

read -p "version: " ver
git subtree split --prefix=Assets/LiteQuark.UI --branch LiteQuark.UI
git tag "ui/$ver" LiteQuark.UI
git push origin LiteQuark.UI --tags
