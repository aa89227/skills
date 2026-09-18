# Design System Handoff

本 reference 是 `github-project-workflow` 與 `design-system` 之間的 handoff 規則來源。
它只描述 approval、routing、metadata 與 lifecycle gate；GitHub Project Status 仍由
[lifecycle.md](lifecycle.md) 定義，不能用本 reference 增加新的 Status。

## 兩種 approval 必須分開

### Requirement Approval

`Requirement Approval` 是 GitHub Issue 的需求核准，代表目前的使用者需求、範圍、
acceptance criteria 與重要限制已被 Human 核准。它由 `Specifying` → `Ready` 的直接
Human Project Status 變更證明。

### Design System Approval

`Design System Approval` 是目前 Design System contract packet 的核准，必須同時包含：

1. 已由使用者確認的 `Experience Brief`；
2. 已核准的 `Component Specification`；
3. 已記錄且已核准的 Accessibility requirements / `Accessibility Specification`；
4. 已建立、完成 mapping，且已核准的 `Test/Conformance Specification`。

Design System Approval 可以由負責的 Human 在設計文件、Issue 或 design child issue 中留下
可驗證的核准證據。Agent、passing tests、CI、沒有反對意見或 Issue 的 Project Status 都
不能代替這個 approval。

**`Issue Ready` 不等於 `Design System Contract Approved`。**

`Ready` 只代表 Requirement Approval。即使 Issue 已經是 `Ready`，只要當前需求沒有可
驗證的 approved `Component Specification`，就必須先走 `design` mode，不能直接走
`build` mode。

## 跨 skill routing rule

如果 Issue 或使用者請求涉及下列任一內容，先檢查目前需求是否有「針對這個 component、
theme 或 contract 版本」的 approved `Component Specification`：

- `Design System`
- `design tokens`
- `visual rules`
- `theme`
- `component contract`
- `shared CSS`
- `accessibility contract`
- `conformance`
- 建立新的 Design System component

若沒有可驗證的 approved `Component Specification`，Agent **必須先使用 Design System
`design` mode，不得直接使用 `build` mode**。已發布套件的 manifest、Issue `Ready`、
Agent 自己產生的草稿或 passing tests，不能單獨證明 Component Specification 已核准。

若請求只是依照已核准且可定位的既有 contract 實作，才可使用 `build` mode；若是在
consumer repository 使用已發布 component，則依 Design System role 使用 `consume` mode。
若既有 contract 無法定位或需求會改變 shared behavior，回到 `design` mode。

## `design` mode 的固定行為

1. 先對實際 target repository 執行 read-only preflight：

   ```bash
   scripts/resolve_design_system_context.py <target-root> --mode design --format json
   ```

2. 只詢問最多三個尚未確定的視覺或互動問題。問題使用使用者語言，聚焦於：
   - 使用者要完成什麼、畫面何時出現，以及內容放在哪裡；
   - 版面、層級、密度、主題、動態效果或視覺強調；
   - 使用者如何開啟、操作、確認、取消、離開，以及 loading、空白、無法使用、錯誤、
     成功時看見什麼回饋。

   不得要求使用者選擇 token name、CSS variable、component ID、API、ARIA attribute、
   DOM structure、package version 或 test runner。這些是 Agent 與 Design System maintainer
   根據體驗需求整理的內部產物。

3. 產生一份簡短的 `Experience Brief`，並用白話回述理解結果：

   ```text
   Experience: <使用者要完成的事情>
   Visual: <版面、位置、層級、主題、密度、動態>
   Interaction: <開啟、操作、確認、取消、離開>
   States: <loading、空白、無法使用、錯誤、成功或其他可見狀態>
   References: <截圖、設計稿或自然語言例子>
   ```

4. 在使用者確認 Experience Brief 前，不得建立 runtime implementation、tokens、CSS、
   Blazor component、implementation branch 或 implementation PR。可以保留未提交的對話
   草稿，但不得把猜測寫成 shared contract。

5. Experience Brief 確認後，Agent 才能將體驗翻譯成 Component、Accessibility 與
   Test/Conformance Specification 草稿。這仍然不是 Design System Approval；四份 contract
   packet 都完成並由 Human 核准後，才可將 `Design System Approval` 設為 `Approved`。

## `build` mode 的進入條件

只有以下條件全部成立，才能使用 `build` mode 實作 shared Design System artifacts：

- Experience Brief 已確認；
- Component Specification 已核准；
- Accessibility requirements 已記錄；
- Test/Conformance scenarios 已建立並完成 mapping；
- Agent 已輸出 implementation plan 與 validation targets。

前四項形成 `Design System Approval`。第五項是開啟 implementation gate 的必要條件。
passing tests、Agent 自己的判斷、Issue 已經 `Ready` 或已存在的 Draft PR 都不能取代
任何一項條件。

如果 `Scope: Build` 是重用已核准的既有 contract，該 contract 中針對目前行為的已確認
Experience Brief 可以作為 Brief evidence；若無法定位這份 evidence，仍須先回到 `design`。
重用既有 contract 不需要重新詢問已明確的體驗，但不能省略 gate check、implementation plan
或 validation targets。

## Workflow Metadata

以下四個值是 Issue 的 `Workflow Metadata`，不是 Project Status，也不是新的 lifecycle：

```text
Design System Scope: None | Design | Build | Design+Build
Design System Phase: Not Required | Experience Intake | Contract Draft | Contract Approved
Design System Approval: Pending | Approved
Implementation Gate: Blocked | Open
```

### 欄位定義

| 欄位 | 值 | 意義 |
| --- | --- | --- |
| `Design System Scope` | `None` | 請求沒有 shared tokens、visual rules、theme、component contract、shared CSS、accessibility contract、conformance 或新 Design System component。依一般 workflow 實作。 |
|  | `Design` | 這個 Issue 只要定義或修改體驗與 shared contract，不包含 runtime implementation。 |
|  | `Build` | 這個 Issue 只實作已存在且已核准的 contract。若找不到 approved Component Specification，必須改走 `design`。 |
|  | `Design+Build` | 同一個 Issue 同時包含新 contract 設計與 runtime implementation；必須先完成 `design`，再完成 `build`。 |
| `Design System Phase` | `Not Required` | `Design System Scope: None` 的唯一 phase 值；不需要 Design System handoff。 |
|  | `Experience Intake` | 體驗問題尚未釐清，或 Experience Brief 尚未由使用者確認。 |
|  | `Contract Draft` | Brief 已確認，但 Component、Accessibility 或 Test/Conformance Specification 尚未全部核准。 |
|  | `Contract Approved` | contract packet 的四項 approval 都已完成；對 Build scope 也代表既有 contract 已被驗證為目前版本。 |
| `Design System Approval` | `Pending` | 任一必要 artifact 尚未核准；對 `Scope: None` 表示「不適用」，不是阻擋一般實作的原因。 |
|  | `Approved` | Experience Brief、Component Specification、Accessibility Specification 與 Test/Conformance Specification 都有可驗證的 Human approval。 |
| `Implementation Gate` | `Blocked` | 不得開始被 gate 保護的 runtime implementation；不得建立 implementation branch、修改 runtime code 或建立 implementation PR。 |
|  | `Open` | 目前 scope 的 implementation prerequisites 已滿足，可以依 lifecycle transition 開始下一階段；`Scope: None` 仍只受一般 Requirement Approval 約束。 |

### 欄位組合與 Project Status

- `Design System Scope: None` 必須使用 `Design System Phase: Not Required`。它不要求
  `Design System Approval`；通常在一般 requirement 已 `Ready` 且準備開始實作時把
  `Implementation Gate` 設為 `Open`。
- `Design System Scope: Build` 或 `Design+Build` 在 contract packet 未完成時必須是
  `Design System Approval: Pending` 與 `Implementation Gate: Blocked`。
- `Design System Phase: Contract Approved` 與 `Design System Approval: Pending` 不可同時
  表示同一個 contract packet；如果 approval 被撤回或需求改變，回到 `Contract Draft`
  與 `Pending`。
- `Implementation Gate: Open` 不是 Project Status。只有在 gate check 通過後，才可依
  [lifecycle.md](lifecycle.md) 的既有 `Ready` → `In Progress` transition。不能建立
  `Designing`、`Contract Review`、`Design Approved` 或其他自訂 lifecycle Status。
- `Issue Ready` 可以與 `Experience Intake`、`Contract Draft`、`Pending` 並存；這表示
  Requirement Approval 已完成，但 Design System handoff 尚未完成。此時保持 `Ready`，
  不得把它當成可以直接 coding 的訊號。
- 如果設計討論改變了需求、範圍、acceptance criteria 或重要限制，依一般規則回到
  `Specifying`；不要用 metadata 掩蓋 material requirement change。

## `Ready` → `In Progress` gate check

變更 Project Status 前，Agent 必須重新讀取 Issue body、Workflow Metadata、Project Status、
相關 contract 與 PR/branch facts，並執行以下 gate check：

1. 所有 scope 都必須是 `Implementation Gate: Open` 才能進入 `In Progress`。對
   `Scope: None`，這只是一般 lifecycle gate；對 Design System scope，以下規則再限制
   `Open` 可以授權的 artifact 類型。
2. `Design System Scope: None`：Design System gate 不適用；Requirement Approval 與一般
   lifecycle entry criteria 滿足後，可進入 `In Progress`。
3. `Design System Scope: Design`：如果只是設計 artifact，必須使用 `design` mode；
   `Implementation Gate: Open` 只代表可以開始核准的 design artifact work，不代表可以
   `build` 或建立 runtime implementation。`Blocked` 時不可建立 runtime implementation；
   要開始 repository artifact work，仍須依當前 Issue 的 Work Mode 與 approval 規則核准，
   不能把 gate 當成新的 Project Status。
4. `Design System Scope: Build` 或 `Design+Build`：
   - `Implementation Gate: Blocked` 時，不得進入 `In Progress`，不得建立 implementation
     branch、修改 runtime code 或建立 implementation PR；
   - 只有 `Implementation Gate: Open`，且完整 build entry conditions 都成立時，才能進入
     `In Progress` 並使用 `build` mode。

如果 gate 不通過，保留目前合法的 Project Status，記錄 `Blocked`、`Blocked By` 與
`Unblocking Condition`；不要新增 lifecycle Status。`Ready` 也不能被 passing tests、
Agent 自己的判斷或 Issue 已經 Ready 轉換成 Design System Approval。

## 混合型 Issue

同一個 Issue 同時包含 Design System 設計與實作時，固定順序是：

1. 先完成 Experience Intake 與 `design` mode；
2. 確認 Experience Brief，完成並核准 contract packet；
3. 輸出 implementation plan 與 validation targets，將 gate 設為 `Open`；
4. 再用 `build` mode 實作，依既有 lifecycle 進入 `In Progress`。

如果兩個階段各自可驗證、可審查或內容太大，建議用 GitHub 原生 parent/sub-issue 建立
`design child issue` 與 `implementation child issue`。不要因為原 Issue 已經 `Ready` 就
跳過 design phase；也不要建立與既有 lifecycle 衝突的 `Designing` 或 `Contract Approved`
Project Status。設計階段用這四個 Workflow Metadata 值或 child issue 表達，不改動 Status
詞彙。

## 修改前後 routing table

| 情境 | 修改前行為 | 修改後行為 |
| --- | --- | --- |
| Issue/請求含 Design System 關鍵內容，但沒有 approved Component Specification | 可能把 `Ready` 當作 contract approval，直接進 `build`。 | 強制先走 `design`，先跑 `--mode design` preflight，最多問三個白話體驗問題。 |
| Issue `Ready` | 只被解讀為整個工作可直接開始。 | 只代表 Requirement Approval；明確標記 `Issue Ready` 不等於 `Design System Contract Approved`。 |
| Design mode | 有 Experience Intake 與內部翻譯，但沒有明確的 brief confirmation、artifact block 與 gate。 | 先回述 Experience Brief；確認前不得 runtime/tokens/CSS/component/implementation PR。 |
| Build mode | 只要 Component Specification 與 conformance mapping 看似存在就可能開始。 | 必須同時有 Brief、approved Component、Accessibility requirements、Test/Conformance mapping、implementation plan 與 validation targets。 |
| `Ready` → `In Progress` | 沒有 Design System gate check。 | `Scope: None` 走一般流程；其餘必須 `Implementation Gate: Open`，否則不能建立 implementation branch 或 runtime code。 |
| Design+Build Issue | 可把設計與實作視為同一個 Ready-to-code 動作。 | 先 design、再 build；內容大或可分別驗證時建議 design/implementation child issues。 |

## 可驗證 workflow examples

### Example 1：Issue 已 `Ready`，但尚未有 Design System contract

- **Mode**：`design`，並先執行 `scripts/resolve_design_system_context.py <target-root> --mode design --format json`。
- **是否詢問使用者**：是，只問最多三個尚未確定的體驗問題，例如畫面位置、使用者操作、loading/錯誤回饋；不問 token、API 或 ARIA。
- **是否可以修改 repository**：Experience Brief 未確認前，不可修改 runtime code、tokens、CSS 或 component；只可保留未提交的 brief 草稿。確認後可以建立 contract 草稿，但仍不可開始 runtime implementation。
- **是否可以建立 branch 或 PR**：不能建立 implementation branch 或 implementation PR。若設計 artifact 需要獨立審查，應使用 design child issue 的既有流程，不得把它當成 runtime implementation PR。
- **Project Status**：保持 `Ready`；metadata 使用 `Design System Scope: Design+Build`、`Design System Phase: Experience Intake` 或 `Contract Draft`、`Design System Approval: Pending`、`Implementation Gate: Blocked`。若設計討論改變需求，回到 `Specifying`。

### Example 2：Issue 已 `Ready`，且 Component Specification 已核准

- **前提**：除了 Component Specification 外，Experience Brief 已確認、Accessibility requirements 已記錄、Test/Conformance scenarios 已建立並完成 mapping，且 implementation plan 與 validation targets 已輸出。
- **Mode**：`build`；先完成一般 context resolution 與 gate check，不再重問已確認的體驗問題。
- **是否詢問使用者**：不需要，除非發現目前 request 與核准 contract 有矛盾；矛盾時停止 build 並回到 `design` 或 `Specifying`。
- **是否可以修改 repository**：可以修改核准 contract 所涵蓋的 runtime implementation、tests 與必要文件；不得在 build 中自行發明未核准行為。
- **是否可以建立 branch 或 PR**：可以在 gate open 後建立 implementation branch 與 Draft PR，並依 workflow 進行 review；不得在 gate open 前預先建立 runtime implementation PR。
- **Project Status**：`Design System Phase: Contract Approved`、`Design System Approval: Approved`、`Implementation Gate: Open` 後，才能由 `Ready` 進入 `In Progress`。若只有 Component Specification 而其餘條件缺少，這個 example 不成立，必須回到 `design`。

### Example 3：使用者只要求依照既有 contract 實作 component

- **Mode**：shared Design System repository 使用 `build`；consumer repository 使用 `consume`。先定位目前 contract、host 與版本，不因為請求提到 component 就重新設計。
- **是否詢問使用者**：通常不需要；只有 contract、目標 host 或可見行為無法定位時，才用白話詢問必要的體驗澄清，不要求技術選擇。
- **是否可以修改 repository**：可以在正確 target repository 修改 contract 已涵蓋的 implementation 與對應驗證；consumer 不得複製 shared source 或改寫 shared contract。
- **是否可以建立 branch 或 PR**：當既有 contract 可驗證、`Implementation Gate: Open` 且 Issue 已滿足 Requirement Approval 後，可以建立 implementation branch/Draft PR；consumer 的 shared gap 應轉為 change proposal。
- **Project Status**：若 `Scope: Build`，使用 `Contract Approved`、`Approved`、`Open`，然後 `Ready` → `In Progress`。若是 consumer composition，按 consumer workflow 處理，不把使用既有 component 誤分類成新 contract design。

### Example 4：同時要求設計新 theme 與實作 theme toggle

- **Mode**：先 `design`，完成 theme 的視覺/互動決策與 contract packet，再 `build` 實作 theme toggle；不得一開始直接 `build`。
- **是否詢問使用者**：需要時最多問三個白話問題，例如主題切換時機、明暗主題的層級差異、切換後如何讓使用者知道結果；不問 token name、CSS variable 或 component API。
- **是否可以修改 repository**：Experience Brief 確認前不可修改 runtime/tokens/CSS/component。Contract Approved 且 gate open 後，可以依 plan 修改 theme tokens、shared CSS、toggle component 與 tests。
- **是否可以建立 branch 或 PR**：設計 gate blocked 時不可建立 implementation branch 或 implementation PR；完成 approval packet、plan 與 targets 後才可建立 implementation branch/Draft PR。
- **Project Status**：混合型 Issue 可先保持 `Ready` 並以 `Experience Intake`/`Contract Draft`、`Pending`、`Blocked` 表達 handoff；完成 contract 後設為 `Contract Approved`、`Approved`、`Open`，再進 `In Progress`。若範圍太大或兩階段可獨立驗證，建立 design child issue 與 implementation child issue，但仍只用既有 Project Status。
