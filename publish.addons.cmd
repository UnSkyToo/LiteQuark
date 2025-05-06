@echo off
set /p ver=version:
git subtree split --prefix=Assets/LiteQuark.Addons --branch LiteQuark.Addons
git tag addons/%ver% LiteQuark.Addons
git push origin LiteQuark.Addons --tags
pause