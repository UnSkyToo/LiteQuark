#!/bin/bash

read -p "version: " ver
git subtree split --prefix=Assets/LiteQuark --branch LiteQuark
git tag "$ver" LiteQuark
git push origin LiteQuark --tags
