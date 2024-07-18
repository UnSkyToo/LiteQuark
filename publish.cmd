@echo off
set /p ver=version:
git subtree split --prefix=Assets/LiteQuark --branch LiteQuark
git tag ver LiteQuark
git push origin LiteQuark --tags
pause