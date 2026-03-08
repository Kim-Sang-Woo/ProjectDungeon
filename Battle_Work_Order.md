# Battle Work Order (v0.1)

## 0) 목표
- 이벤트에서 전투를 호출하고, Encounter Data 기반으로 몬스터를 배치한다.
- 라운드 상태 머신으로 전투를 안정적으로 구동한다.
- 초기에 디폴트 카드(공격) 1장으로 E2E 승/패 루프를 완성한다.

---

## 1) 구현 순서 (안정형)
1. 데이터 모델 확정
2. 전투 상태머신(BattleManager)
3. 수치 계산 모듈(BattleMath)
4. UI 없이 최소 루프(E2E 로그 기반)
5. BattleUI 골격/레이아웃 코드
6. 입력/카드 사용/턴 종료 조건
7. 승리 결과 + 보상 연결
8. 예외/경계값 테스트

---

## 2) 이번 스프린트 범위 (Start Coding)
- [x] MonsterData ScriptableObject
- [x] EncounterData ScriptableObject
- [x] BattleMath (Damage 공식)
- [x] BattleManager 스켈레톤 + 상태 전이 뼈대
- [x] EventEffect에서 전투 호출 효과 추가 (StartBattleEffect)
- [x] BattleUI 코드 레이아웃 (골격)

---

## 3) 핵심 규칙(현재 합의)
- 최종 피해량: `FinalDamage = DamageConst * (1 + DamagePer / 100)`
- 플레이어 턴 종료 조건:
  1) Mana <= 0
  2) Hand == 0
  3) 사용 가능한 카드 없음
  4) 턴 종료 버튼 클릭
- 몬스터 피격(플레이어 기준) 처리 우선순위:
  1) Dodge 소모 회피
  2) Shield로 흡수
  3) HP 감소

---

## 4) 필요한 이미지 에셋 목록

### A. 전투 배경/UI 프레임
- battle_bg_main (전장 배경)
- battle_top_bar_bg
- battle_bottom_bar_bg
- panel_dark_9slice (공용 패널)
- slot_frame_normal / slot_frame_hover / slot_frame_selected

### B. 플레이어 리소스/상태 아이콘
- icon_hp_heart
- icon_shield
- icon_dodge
- icon_mana_orb
- icon_deck
- icon_discard
- icon_endturn

### C. 몬스터 관련
- monster_{id}_portrait (몬스터 초상)
- monster_target_ring (타겟 표시)
- monster_hit_fx (피격 이펙트 스프라이트 시트 optional)

### D. 카드 관련
- card_frame_attack
- card_frame_skill
- card_frame_power (확장 대비)
- card_back
- card_art_default_attack
- mana_cost_badge

### E. 전투 텍스트/숫자 연출
- dmg_number_font_sprite (optional)
- heal_number_font_sprite (optional)
- miss_text_sprite (optional)

### F. 버튼/공용 아이콘
- btn_primary_normal / hover / pressed
- btn_endturn_normal / hover / pressed
- icon_coin (상단 우측 자원표시용)

---

## 5) 확인 포인트
- ScriptableObject 생성 메뉴에서 데이터 에셋 생성 가능해야 함.
- BattleManager는 EncounterData를 받아 몬스터 런타임 목록을 구성해야 함.
- Damage 공식은 BattleMath로 단일화.
