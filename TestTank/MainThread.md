﻿graph TD
A[MongoDB 数据框架] --> B[业务模块基类]
B --> C[后台服务模块]
B --> D[用户业务模块]
E[启动初始化器] --> C
F[用户登录服务] --> D
G[优雅关闭管理器] --> C & D
H[DI 容器] -->|注册| A & B & E & F & G