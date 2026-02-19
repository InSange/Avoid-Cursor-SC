using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 'Play' 버튼을 PlayerCursor가 클릭할 수 있도록 처리합니다.
/// 'Button' 컴포넌트 대신 이 스크립트를 사용합니다.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Image))]
public class PlayButtonHandler : BaseCursorButton
{
}