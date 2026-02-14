# Changelog

## **[0.4.0] - 2026-02-14**
- 修复：音频加载失败时 Carrier 未释放的 BUG
- 修复：HttpClient.SendBatch 在非主线程使用 UnityWebRequest 的报错
- 修复：EffectObject 加载中 Stop 后未回收 GameObject 的 BUG

## **[0.3.0] - 2026-02-09**
-- 优化：System相关设置放到自己的Package内

## **[0.2.1] - 2026-01-07**
- 新增：音效系统增加音量设置接口
- 新增：音效系统增加音效获取接口

## **[0.2.0] - 2025-10-09**
- 更新：配合主包进行版本升级
- 新增：引入 UniTask

## **[0.1.0-preview] - 2025-02-25**
- 新增：从 LiteQuark 主体中拆分出的扩展库初版
