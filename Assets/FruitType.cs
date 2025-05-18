// FruitType.cs
using UnityEngine; // Sprite를 사용하기 위해 필요할 수 있음 (FruitSpriteMapping에서)

// 여기에 FruitType Enum이 이미 정의되어 있어야 합니다.
// public enum FruitType { None, 귤, 바나나, ... } // 이전 단계에서 만듦
// 이 아래에 있는 내용만 남기거나, 새로 만드셨다면 public enum FruitType { ... } 부분만 작성합니다.
// using UnityEngine; 등은 필요 없습니다.

public enum FruitType
{
    None,        // 아무것도 아님 (혹은 빈칸 표시용)
    귤,
    바나나,
    키위,        // 스크립트에 '키'로 되어있던 것을 풀네임으로 변경
    파인애플,    // 스크립트에 '파'로 되어있던 것을 풀네임으로 변경
    딸기,
    토마토,      // 스크립트에 '토'로 되어있던 것을 풀네임으로 변경
    샤인머스켓,  // 스크립트에 '샤'로 되어있던 것을 풀네임으로 변경
    체리,
    블루베리,    // 스크립트에 '블'로 되어있던 것을 풀네임으로 변경

    // --- 손님별 특별 아이템 ---
    뼈다귀쿠키, // 뭉뭉이 주문
    풀,         // 메메 주문
    당근,       // 토토 주문
    나비장식,   // 크크 주문 (나비 -> 나비장식으로 명확히)
    생선장식,   // 냥냥 주문 (생선 -> 생선장식으로 명확히)
    치즈        // 찍찍이 주문
    // 필요에 따라 게임에 등장할 모든 과일/아이템 종류를 여기에 추가합니다.
}

// 여기에 FruitType Enum이 이미 정의되어 있어야 합니다.
// public enum FruitType { None, 귤, 바나나, ... } // 이전 단계에서 만듦

public enum GameState
{
    TutorialDisplay,
    Playing,
    Paused,
    GameOver
}