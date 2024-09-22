# Robot Safety System Using Body Tracking
## Overview
이 프로젝트는 3D 카메라와 Nuitrack SDK를 이용하여 작업자의 신체를 추적하고, 로봇의 위치를 실시간으로 모니터링하여 작업자와 로봇 사이의 거리에 따라 로봇의 속도를 제어함으로써 작업자의 안전을 보장하는 시스템입니다. 작업자가 로봇에 가까워질 경우 로봇의 동작 속도를 줄이고, 최종적으로 정지하도록 설계했습니다.

## Key Features
- 바디 트래킹: 3D 카메라를 사용하여 작업자의 신체 정보를 실시간으로 추적합니다.
- 로봇 속도 제어: Python 서버가 로봇의 동작 상태를 감지하고, 클라이언트(C# Body Tracking)에서 전달받은 작업자와의 거리 정보를 바탕으로 로봇 속도를 조절합니다.
- 실시간 통신: Python은 로봇과 통신하며 실시간으로 모니터링 및 제어하고, 동시에 C#과 통신하며 작업자와의 거리 정보를 받습니다.
- 시각화: Unity를 이용하여 신체 추적 정보를 시각화하여 시스템의 동작을 직관적으로 확인할 수 있습니다.

## Run Image
<img src="https://github.com/user-attachments/assets/e8a2496e-837c-447d-b33e-0cf226b70536">
<img src="https://github.com/user-attachments/assets/96c0dc9b-2e74-454f-9e28-b3d84f45abe4">

## License
이 프로젝트는 Apache License 2.0에 따라 배포됩니다.
