<script setup>
import { ref, reactive, computed, onMounted, onUnmounted, watch, nextTick } from 'vue';
import RegisterModal from './components/RegisterModal.vue';
import CheckoutModal from './components/CheckoutModal.vue';
import DetailModal from './components/DetailModal.vue';
import TodayVisitorsModal from './components/TodayVisitorsModal.vue';
import SettingsModal from './components/SettingsModal.vue';

// ─── i18n Dictionary ─────────────────────────────────────────────────────────
const LANGS = { zh: "中文", en: "English", vi: "Tiếng Việt" };

const T = {
  zh: {
    sysTitle: "WAREHOUSE ACCESS", sysSub: "成品倉庫門禁管理系統",
    register: "+ 訪客登記", monitor: "即時監控", history: "歷史紀錄",
    settings: "設定",
    statsInside: "目前在場", statsToday: "今日訪客", statsVendor: "廠商",
    statsBrand: "品牌客戶", statsAudit: "稽核人員",
    searchPlaceholder: "搜尋姓名、公司、接洽人…",
    allTypes: "全部類型", allStatus: "全部狀態", inside: "在場中", exited: "已離場",
    exportCSV: "↓ 匯出 CSV",
    noVisitors: "目前無在場訪客", noRecords: "無符合紀錄",
    total: "共", records: "筆紀錄",
    checkout: "登記離場", details: "詳情",
    cols: ["編號","姓名","公司","類型","接洽","事由","進場","離場","停留","操作"],
    regTitle: "訪客登記入場", step: "步驟", of: "/",
    stepLabels: ["基本資料","拍照存檔"],
    name: "姓名", company: "公司／單位", empId: "員工工號",
    dept: "部門", contact: "接洽人員", purpose: "事由", note: "備註（選填）",
    required: "必填", lookupBtn: "查詢", lookupFail: "查無此工號",
    nextPhoto: "下一步：拍照 →", back: "← 返回", confirmEntry: "✓ 確認進場",
    skip: "略過", cancel: "取消", retake: "重拍",
    photoHint: "點擊開啟相機拍照", photoSub: "建議拍攝正面照片",
    photoTaken: "✓ 已拍照", camLabel: "訪客拍照存檔",
    camError: "無法存取相機，請確認瀏覽器權限或使用 HTTPS", skipPhoto: "略過拍照",
    checkoutTitle: "離場確認", confirmCheckout: "確認離場",
    exitPhotoHint: "拍攝離場照片（選填）",
    detailTitle: "訪客詳情", close: "關閉",
    entryPhoto: "進場照片", exitPhoto: "離場照片",
    noEntryPhoto: "無進場照片", noExitPhoto: "無離場照片",
    detailFields: ["編號","姓名","公司/單位","類型","部門","接洽人","事由","進場時間","離場時間","停留時間","備註"],
    stillInside: "在場中",
    settingsTitle: "系統設定", contactSection: "接洽人員設定",
    contactNameLabel: "接洽人姓名", contactDeptLabel: "所屬部門",
    contactSaved: "✓ 已儲存接洽人設定", saveContact: "儲存設定",
    changeContact: "變更接洽人", changePin: "變更需輸入 PIN 碼",
    pinLabel: "請輸入 PIN 碼（預設 1234）", pinError: "PIN 碼錯誤",
    confirm: "確認", empSection: "員工名冊（示範資料）",
    types: { vendor:"廠商", brand:"品牌客戶", audit:"第三方稽核", staff:"內部員工" },
    toastEntry: "已登記入場", toastExit: "已登記離場", toastCSV: "✓ CSV 已匯出",
    camFront:"前鏡頭", camBack:"後鏡頭", camSwitching:"切換中…", camHintFront:"前鏡頭・適合自助登記", camHintBack:"後鏡頭・適合保全人員拍攝", camSwitchToBack:"切換後鏡頭", camSwitchToFront:"切換前鏡頭",
    
    // Kiosk Translations
    kioskTitle: "自助登記感應終端",
    kioskClose: "✕ 關閉終端 (Close)",
    kioskHint: "請感應 IC 卡或掃描條碼 (Tap IC Card / Scan Barcode)...",
    kioskGetTemp: "申領登記臨時卡 (Get Temp Card)",
    kioskBindTitle: "申領臨時卡登記 (Manual Temp Card Bind)",
    kioskDeptVendor: " (無需填寫)",
    kioskWaitingBind: "等待臨時卡感應綁定...",
    kioskWaitingHint: "請在感應區刷入臨時卡 (如右側模擬卡片 TEMP-888)",
    kioskSimHeader: "💻 測試模擬感應與掃描面板",
    kioskSimSub: "模擬實體 IC 讀卡設備或 Barcode 條碼機在成品倉門口的動作，可用滑鼠點選：",
    kioskSimStaff: "本廠員工 IC 卡 (感應感應器)",
    kioskSimVisitor: "外部訪客 Barcode 條碼單 (掃描條碼機)",
    kioskSimTemp: "成品倉臨時感應卡 (手動綁定)",
    kioskAlertBindSuccess: "臨時卡感應綁定成功",
    kioskAlertBindFail: "臨時卡綁定失敗，請重試",
    kioskAlertName: "請填寫姓名！",
    kioskAlertCompany: "請填寫公司單位！",
    kioskSwipeWelcome: "歡迎進入",
    kioskSwipeCheckout: "離場登記成功",
    kioskSwipeTime: "進場時間",
    kioskSwipeExitTime: "離場時間",
    kioskSwipeDuration: "停留時間",
    kioskSwipeReturnCard: "⚠️ 請將此臨時卡歸還給值班人員！",
    kioskSwipeError: "感應失敗：未檢索到有效卡片",
    kioskSwipeErrorSub: "若您無卡片，請點擊申領臨時卡並手動輸入資料進行綁定。",
    kioskSubmit: "確認申領綁定",
    kioskBack: "← 返回",
    kioskSystemError: "系統連線錯誤",
    kioskSystemErrorSub: "請確認後端伺服器與資料庫是否正常運作。",
  },
  en: {
    sysTitle: "WAREHOUSE ACCESS", sysSub: "Finished Goods Warehouse Access System",
    register: "+ Register Visitor", monitor: "Live Monitor", history: "History",
    settings: "Settings",
    statsInside: "On-site", statsToday: "Today", statsVendor: "Vendor",
    statsBrand: "Brand Client", statsAudit: "Auditor",
    searchPlaceholder: "Search name, company, contact…",
    allTypes: "All Types", allStatus: "All Status", inside: "On-site", exited: "Exited",
    exportCSV: "↓ Export CSV",
    noVisitors: "No visitors on-site", noRecords: "No matching records",
    total: "Total", records: "records",
    checkout: "Check Out", details: "Details",
    cols: ["ID","Name","Company","Type","Contact","Purpose","Entry","Exit","Duration","Action"],
    regTitle: "Visitor Registration", step: "Step", of: "/",
    stepLabels: ["Basic Info","Photo"],
    name: "Full Name", company: "Company / Unit", empId: "Employee ID",
    dept: "Department", contact: "Contact Person", purpose: "Purpose", note: "Note (optional)",
    required: "Required", lookupBtn: "Lookup", lookupFail: "Employee not found",
    nextPhoto: "Next: Photo →", back: "← Back", confirmEntry: "✓ Confirm Entry",
    skip: "Skip", cancel: "Cancel", retake: "Retake",
    photoHint: "Tap to open camera", photoSub: "Please face the camera",
    photoTaken: "✓ Photo taken", camLabel: "Visitor Photo",
    camError: "Camera unavailable. Check browser permissions or use HTTPS.", skipPhoto: "Skip Photo",
    checkoutTitle: "Confirm Exit", confirmCheckout: "Confirm Exit",
    exitPhotoHint: "Take exit photo (optional)",
    detailTitle: "Visitor Details", close: "Close",
    entryPhoto: "Entry Photo", exitPhoto: "Exit Photo",
    noEntryPhoto: "No entry photo", noExitPhoto: "No exit photo",
    detailFields: ["ID","Name","Company","Type","Dept","Contact","Purpose","Entry Time","Exit Time","Duration","Note"],
    stillInside: "On-site",
    settingsTitle: "System Settings", contactSection: "Contact Person Settings",
    contactNameLabel: "Contact Name", contactDeptLabel: "Department",
    contactSaved: "✓ Contact settings saved", saveContact: "Save Settings",
    changeContact: "Change Contact", changePin: "PIN required to change",
    pinLabel: "Enter PIN (default 1234)", pinError: "Incorrect PIN",
    confirm: "Confirm", empSection: "Employee Directory (Demo Data)",
    types: { vendor:"Vendor", brand:"Brand Client", audit:"3rd Party Auditor", staff:"Internal Staff" },
    toastEntry: "checked in", toastExit: "checked out", toastCSV: "✓ CSV exported",
    camFront:"Front Cam", camBack:"Rear Cam", camSwitching:"Switching…", camHintFront:"Front camera · Self-registration", camHintBack:"Rear camera · Guard-assisted", camSwitchToBack:"Switch to Rear", camSwitchToFront:"Switch to Front",
    
    // Kiosk Translations
    kioskTitle: "Kiosk Gate Terminal",
    kioskClose: "✕ Close Terminal",
    kioskHint: "Tap IC Card / Scan Barcode...",
    kioskGetTemp: "Get Temp Card",
    kioskBindTitle: "Manual Temp Card Bind",
    kioskDeptVendor: " (Not Required)",
    kioskWaitingBind: "Waiting for Card Swipe to Bind...",
    kioskWaitingHint: "Please tap your card on the sensor (e.g., TEMP-888 on the right)",
    kioskSimHeader: "💻 Sensors Simulator Panel",
    kioskSimSub: "Simulate physical IC card readers or barcode scanners at the warehouse gate:",
    kioskSimStaff: "Employee IC Cards (Tap Sensor)",
    kioskSimVisitor: "Visitor Barcode Passes (Scan)",
    kioskSimTemp: "Temporary IC Cards (Manual Bind)",
    kioskAlertBindSuccess: "Temporary Card Bound Successfully",
    kioskAlertBindFail: "Failed to bind temporary card. Please try again.",
    kioskAlertName: "Please fill in Name!",
    kioskAlertCompany: "Please fill in Company!",
    kioskSwipeWelcome: "Welcome",
    kioskSwipeCheckout: "Checked Out Successfully",
    kioskSwipeTime: "Entry Time",
    kioskSwipeExitTime: "Exit Time",
    kioskSwipeDuration: "Stay Duration",
    kioskSwipeReturnCard: "⚠️ Please return this temporary card to the duty officer!",
    kioskSwipeError: "Scan Failed: No valid card detected",
    kioskSwipeErrorSub: "If you have no card, please click Request Temp Card and enter details to bind.",
    kioskSubmit: "Confirm Bind",
    kioskBack: "← Back",
    kioskSystemError: "System Connection Error",
    kioskSystemErrorSub: "Please verify that the backend server and database are running.",
  },
  vi: {
    sysTitle: "WAREHOUSE ACCESS", sysSub: "Hệ Thống Quản Lý Ra Vào Kho Thành Phẩm",
    register: "+ Đăng Ký Khách", monitor: "Giám Sát Trực Tiếp", history: "Lịch Sử",
    settings: "Cài Đặt",
    statsInside: "Đang Trong Kho", statsToday: "Khách Hôm Nay", statsVendor: "Nhà Cung Cấp",
    statsBrand: "Khách Hàng", statsAudit: "Kiểm Toán",
    searchPlaceholder: "Tìm tên, công ty, người liên hệ…",
    allTypes: "Tất Cả Loại", allStatus: "Tất Cả Trạng Thái", inside: "Trong Kho", exited: "Đã Ra",
    exportCSV: "↓ Xuất CSV",
    noVisitors: "Không có khách trong kho", noRecords: "Không có kết quả phù hợp",
    total: "Tổng", records: "bản ghi",
    checkout: "Đăng Ký Ra", details: "Chi Tiết",
    cols: ["Mã","Họ Tên","Công Ty","Loại","Người LH","Mục Đích","Vào","Ra","Thời Gian","Thao Tác"],
    regTitle: "Đăng Ký Vào Kho", step: "Bước", of: "/",
    stepLabels: ["Thông Tin","Chụp Ảnh"],
    name: "Họ và Tên", company: "Công Ty / Đơn Vị", empId: "Mã Nhân Viên",
    dept: "Phòng Ban", contact: "Người Liên Hệ", purpose: "Mục Đích", note: "Ghi Chú (tùy chọn)",
    required: "Bắt buộc", lookupBtn: "Tra Cứu", lookupFail: "Không tìm thấy mã nhân viên",
    nextPhoto: "Tiếp Theo: Chụp Ảnh →", back: "← Quay Lại", confirmEntry: "✓ Xác Nhận Vào",
    skip: "Bỏ Qua", cancel: "Hủy", retake: "Chụp Lại",
    photoHint: "Nhấn để mở camera", photoSub: "Vui lòng hướng mặt vào camera",
    photoTaken: "✓ Đã chụp ảnh", camLabel: "Chụp Ảnh Khách",
    camError: "Không thể truy cập camera. Kiểm tra quyền trình duyệt hoặc dùng HTTPS.", skipPhoto: "Bỏ Qua Ảnh",
    checkoutTitle: "Xác Nhận Ra Kho", confirmCheckout: "Xác Nhận Ra",
    exitPhotoHint: "Chụp ảnh ra kho (tùy chọn)",
    detailTitle: "Chi Tiết Khách", close: "Đóng",
    entryPhoto: "Ảnh Vào", exitPhoto: "Ảnh Ra",
    noEntryPhoto: "Không có ảnh vào", noExitPhoto: "Không có ảnh ra",
    detailFields: ["Mã","Họ Tên","Công Ty","Loại","Phòng Ban","Người LH","Mục Đích","Giờ Vào","Giờ Ra","Thời Gian","Ghi Chú"],
    stillInside: "Trong Kho",
    settingsTitle: "Cài Đặt Hệ Thống", contactSection: "Cài Đặt Người Liên Hệ",
    contactNameLabel: "Tên Người Liên Hệ", contactDeptLabel: "Phòng Ban",
    contactSaved: "✓ Đã lưu cài đặt người liên hệ", saveContact: "Lưu Cài Đặt",
    changeContact: "Thay Đổi Người Liên Hệ", changePin: "Cần PIN để thay đổi",
    pinLabel: "Nhập PIN (mặc định 1234)", pinError: "PIN không đúng",
    confirm: "Xác Nhận", empSection: "Danh Sách Nhân Viên (Dữ Liệu Demo)",
    types: { vendor:"Nhà Cung Cấp", brand:"Khách Hàng", audit:"Kiểm Toán Bên Thứ 3", staff:"Nhân Viên Nội Bộ" },
    toastEntry: "đã vào kho", toastExit: "đã ra kho", toastCSV: "✓ Đã xuất CSV",
    camFront:"Camera Trước", camBack:"Camera Sau", camSwitching:"Đang chuyển…", camHintFront:"Camera trước · Tự đăng ký", camHintBack:"Camera sau · Bảo vệ hỗ trợ", camSwitchToBack:"Chuyển sang Sau", camSwitchToFront:"Chuyển sang Trước",
    
    // Kiosk Translations
    kioskTitle: "Trạm Đăng Ký Tự Động",
    kioskClose: "✕ Đóng Trạm",
    kioskHint: "Quét thẻ IC / Quét mã vạch...",
    kioskGetTemp: "Yêu Cầu Thẻ Tạm",
    kioskBindTitle: "Liên Kết Thẻ Tạm Thủ Công",
    kioskDeptVendor: " (Không Cần Thiết)",
    kioskWaitingBind: "Đang chờ quét thẻ để liên kết...",
    kioskWaitingHint: "Vui lòng quét thẻ tạm trong vùng cảm biến (Ví dụ: TEMP-888 ở bên phải)",
    kioskSimHeader: "💻 Bảng Mô Phỏng Cảm Biến",
    kioskSimSub: "Mô phỏng đầu đọc thẻ IC hoặc máy quét mã vạch vật lý tại cổng kho:",
    kioskSimStaff: "Thẻ IC Nhân Viên (Quét Cảm Biến)",
    kioskSimVisitor: "Thẻ Quét Mã Vạch Khách (Quét)",
    kioskSimTemp: "Thẻ Cảm Biến Tạm Thời (Liên Kết Thủ Công)",
    kioskAlertBindSuccess: "Liên kết thẻ tạm thời thành công",
    kioskAlertBindFail: "Liên kết thẻ tạm thất bại, vui lòng thử lại.",
    kioskAlertName: "Vui lòng điền Họ Tên!",
    kioskAlertCompany: "Vui lòng điền Công Ty!",
    kioskSwipeWelcome: "Chào Mừng Vào Kho",
    kioskSwipeCheckout: "Đăng ký ra thành công",
    kioskSwipeTime: "Giờ Vào",
    kioskSwipeExitTime: "Giờ Ra",
    kioskSwipeDuration: "Thời Gian Ra",
    kioskSwipeReturnCard: "⚠️ Vui lòng trả lại thẻ tạm này cho nhân viên trực!",
    kioskSwipeError: "Quét Thất Bại: Không phát hiện thẻ hợp lệ",
    kioskSwipeErrorSub: "Nếu không có thẻ, nhấp vào Yêu cầu thẻ tạm thời và nhập thông tin để liên kết.",
    kioskSubmit: "Xác Nhận Liên Kết",
    kioskBack: "← Quay Lại",
    kioskSystemError: "Lỗi Kết Nối Hệ Thống",
    kioskSystemErrorSub: "Vui lòng xác minh máy chủ phụ trợ và cơ sở dữ liệu đang hoạt động.",
  },
};

const VISITOR_TYPES = ["vendor", "brand", "audit", "staff"];
const TYPE_COLORS = { vendor: "#F97316", brand: "#22C55E", audit: "#EF4444", staff: "#3B82F6" };

// ─── Reactive State ──────────────────────────────────────────────────────────
const lang = ref(localStorage.getItem('lang') || 'zh');
const t = computed(() => T[lang.value]);

const activeTab = ref('monitor'); // 'monitor' | 'history'
const records = ref([]);
const departments = ref([]);
const purposes = ref([]);
const employees = ref({});
const auditLogs = ref([]);
const defaultContact = ref({ name: "張主任", dept: "倉儲部" });

const showRegister = ref(false);
const showCheckout = ref(null); // record object
const showDetail = ref(null); // record object
const showTodayVisitors = ref(false);
const showSettings = ref(false);

// --- SEPARATED Search and Filters (Monitor vs History) ---
const monitorSearch = ref('');
const monitorFilterType = ref('');

// History UI input states (bound to UI elements via v-model)
const historySearchInput = ref('');
const historyFilterTypeInput = ref('');
const filterStatusInput = ref('');
const filterStartDate = ref('');
const filterEndDate = ref('');

// History active applied states (used in computed filter)
const appliedHistorySearch = ref('');
const appliedHistoryFilterType = ref('');
const appliedFilterStatus = ref('');
const appliedStartDate = ref('');
const appliedEndDate = ref('');

function handleHistoryQuery() {
  appliedHistorySearch.value = historySearchInput.value;
  appliedHistoryFilterType.value = historyFilterTypeInput.value;
  appliedFilterStatus.value = filterStatusInput.value;
  appliedStartDate.value = filterStartDate.value;
  appliedEndDate.value = filterEndDate.value;
}

// --- Toast States ---
const toasts = ref([]);
function showToast(message) {
  const id = Date.now();
  toasts.value.push({ id, message });
  setTimeout(() => {
    toasts.value = toasts.value.filter(t => t.id !== id);
  }, 3500);
}

// ─── Formatted Helpers ───────────────────────────────────────────────────────
function formatTime(ts) {
  if (!ts) return "—";
  const d = new Date(ts);
  return d.toLocaleString("zh-TW", { hour12: false, month: "2-digit", day: "2-digit", hour: "2-digit", minute: "2-digit", second: "2-digit" });
}

function formatSimpleTime(ts) {
  if (!ts) return "—";
  const d = new Date(ts);
  const pad = (n) => n.toString().padStart(2, '0');
  return `${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`;
}

function formatDuration(ms) {
  if (!ms || ms < 0) return "—";
  const m = Math.floor(ms / 60000), h = Math.floor(m / 60);
  return h > 0 ? `${h}h ${m % 60}m` : `${m}m`;
}

function getStayDuration(entryTime) {
  const ms = Date.now() - entryTime;
  const m = Math.floor(ms / 60000), h = Math.floor(m / 60);
  return h > 0 ? `${h}h ${m % 60}m` : `${m}m`;
}

function isLongStay(entryTime) {
  const ms = Date.now() - entryTime;
  return ms > 3600000; // Greater than 1 hour
}

function generateId() {
  return Date.now().toString(36).toUpperCase() + Math.random().toString(36).slice(2, 5).toUpperCase();
}

// ─── Data Fetching ───────────────────────────────────────────────────────────
async function refreshData() {
  try {
    const recsRes = await fetch('/api/records').then(r => r.json());
    records.value = recsRes;

    const deptsRes = await fetch('/api/departments').then(r => r.json());
    departments.value = deptsRes;

    const purposesRes = await fetch('/api/purposes').then(r => r.json());
    purposes.value = purposesRes;

    const empsRes = await fetch('/api/employees').then(r => r.json());
    employees.value = empsRes;

    const settingsRes = await fetch('/api/contact').then(r => r.json());
    defaultContact.value = settingsRes;

    const logsRes = await fetch('/api/audit_logs').then(r => r.json());
    auditLogs.value = logsRes;
  } catch (err) {
    console.error("API error loading datasets:", err);
  }
}

// ─── Filters & Statistics ────────────────────────────────────────────────────
const stats = computed(() => {
  const inside = records.value.filter(r => !r.exitTime);
  const todayStart = new Date().setHours(0, 0, 0, 0);
  const today = records.value.filter(r => r.entryTime >= todayStart || !r.exitTime);

  return {
    onSite: inside.length,
    today: today.length,
    vendor: inside.filter(r => r.type === 'vendor').length,
    brand: inside.filter(r => r.type === 'brand').length,
    audit: inside.filter(r => r.type === 'audit').length
  };
});

// Separated Filters: Historical Records
const filteredRecords = computed(() => {
  return records.value.filter(r => {
    // 1. Search Query
    const query = appliedHistorySearch.value.trim().toLowerCase();
    const matchesSearch = !query || 
      (r.name && r.name.toLowerCase().includes(query)) ||
      (r.company && r.company.toLowerCase().includes(query)) ||
      (r.contact && r.contact.toLowerCase().includes(query));

    // 2. Type Filter
    const matchesType = !appliedHistoryFilterType.value || r.type === appliedHistoryFilterType.value;

    // 3. Status Filter
    const matchesStatus = !appliedFilterStatus.value || 
      (appliedFilterStatus.value === 'inside' ? !r.exitTime : !!r.exitTime);

    // 4. Date Range Filter (Applied on Query button click)
    if (appliedStartDate.value) {
      const startMs = new Date(appliedStartDate.value).setHours(0, 0, 0, 0);
      if (r.entryTime < startMs) return false;
    }
    if (appliedEndDate.value) {
      const endMs = new Date(appliedEndDate.value).setHours(23, 59, 59, 999);
      if (r.entryTime > endMs) return false;
    }

    return matchesSearch && matchesType && matchesStatus;
  });
});

const activeOnSiteVisitors = computed(() => {
  return records.value.filter(r => !r.exitTime);
});

// Separated Filters: Active On-site Monitor Grid
const filteredActiveVisitors = computed(() => {
  return activeOnSiteVisitors.value.filter(r => {
    const query = monitorSearch.value.trim().toLowerCase();
    const matchesSearch = !query || 
      (r.name && r.name.toLowerCase().includes(query)) ||
      (r.company && r.company.toLowerCase().includes(query)) ||
      (r.contact && r.contact.toLowerCase().includes(query));

    const matchesType = !monitorFilterType.value || r.type === monitorFilterType.value;
    return matchesSearch && matchesType;
  });
});

// ─── Handlers ────────────────────────────────────────────────────────────────
async function handleEntry(form) {
  try {
    const res = await fetch('/api/records', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(form)
    }).then(r => r.json());

    if (res.success) {
      showRegister.value = false;
      showToast(`${form.name} ${t.value.toastEntry}`);
      refreshData();
    }
  } catch (e) {
    alert("進場登記失敗，請檢查 API");
  }
}

async function handleCheckout(exitPhoto) {
  if (!showCheckout.value) return;
  const targetId = showCheckout.value.id;
  const targetName = showCheckout.value.name;

  try {
    const res = await fetch(`/api/records/${targetId}/checkout`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ exitPhoto, exitTime: Date.now() })
    }).then(r => r.json());

    if (res.success) {
      showCheckout.value = null;
      showToast(`${targetName} ${t.value.toastExit}`);
      refreshData();
    }
  } catch (e) {
    alert("登記離場失敗，請檢查 API");
  }
}

function exportCSV() {
  const headers = t.value.cols.slice(0, 9).join(',');
  const rows = filteredRecords.value.map(r => {
    const typeLabel = t.value.types[r.type];
    const entryStr = formatTime(r.entryTime);
    const exitStr = r.exitTime ? formatTime(r.exitTime) : t.value.stillInside;
    const durationStr = r.exitTime ? formatDuration(r.exitTime - r.entryTime) : '';

    return `"${r.id}","${r.name}","${r.company}","${typeLabel}","${r.contact}","${r.purpose}","${entryStr}","${exitStr}","${durationStr}"`;
  });

  const csvContent = "\ufeff" + headers + "\n" + rows.join("\n");
  const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.setAttribute("href", url);
  link.setAttribute("download", `warehouse_visitors_${Date.now()}.csv`);
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);

  showToast(t.value.toastCSV);
}

watch(lang, (newLang) => {
  localStorage.setItem('lang', newLang);
});

// ─── Standalone Kiosk Terminal Mode ──────────────────────────────────────────
const showKiosk = ref(false);
const cardInput = ref('');
const kioskStatus = ref({ active: false, success: true, message: '', details: '', returnCardAlert: false });
const showKioskRegister = ref(false);
const waitingForTempCard = ref(false);
const kioskForm = reactive({ name: '', company: '', type: 'vendor', department: '', contact: '', purpose: '業務洽談', note: '' });

function handleKioskTypeChange() {
  if (kioskForm.type === 'vendor') {
    kioskForm.department = '';
  } else if (!kioskForm.department && departments.value.length > 0) {
    kioskForm.department = departments.value[0];
  }
  
  if (kioskForm.type === 'staff') {
    kioskForm.company = '內部員工';
  } else {
    kioskForm.company = '';
  }
}

// --- Kiosk Camera States & Methods ---
const kioskShowCam = ref(false);
const kioskPendingCheckin = ref(null);
const kioskVideoRef = ref(null);
const kioskCanvasRef = ref(null);
const kioskStream = ref(null);
const kioskCamReady = ref(false);
const kioskCamError = ref("");
const kioskCamFacing = ref("user");
const kioskCamSwitching = ref(false);
const kioskHasMultipleCams = ref(false);

async function startKioskCamera(mode) {
  if (kioskStream.value) {
    kioskStream.value.getTracks().forEach(tr => tr.stop());
  }
  kioskCamReady.value = false;
  kioskCamSwitching.value = true;

  try {
    const s = await navigator.mediaDevices.getUserMedia({
      video: { facingMode: mode }
    });
    kioskStream.value = s;
    await nextTick();
    if (kioskVideoRef.value) {
      kioskVideoRef.value.srcObject = s;
      kioskVideoRef.value.onloadedmetadata = () => {
        kioskCamReady.value = true;
        kioskCamSwitching.value = false;
      };
    }
  } catch (e) {
    kioskCamError.value = t.value.camError || "無法存取相機";
    kioskCamSwitching.value = false;
  }
}

function openKioskCamera(pendingData) {
  kioskPendingCheckin.value = pendingData;
  kioskShowCam.value = true;
  kioskCamError.value = "";
  
  navigator.mediaDevices?.enumerateDevices().then(devices => {
    kioskHasMultipleCams.value = devices.filter(d => d.kind === "videoinput").length > 1;
  });
  startKioskCamera("user");
}

function closeKioskCamera() {
  if (kioskStream.value) {
    kioskStream.value.getTracks().forEach(tr => tr.stop());
    kioskStream.value = null;
  }
  kioskShowCam.value = false;
  kioskPendingCheckin.value = null;
}

function flipKioskCamera() {
  const next = kioskCamFacing.value === "user" ? "environment" : "user";
  kioskCamFacing.value = next;
  startKioskCamera(next);
}

function shootKioskPhoto() {
  const v = kioskVideoRef.value;
  const c = kioskCanvasRef.value;
  if (!v || !c) return;
  c.width = v.videoWidth;
  c.height = v.videoHeight;
  const ctx = c.getContext("2d");

  if (kioskCamFacing.value === "user") {
    ctx.translate(c.width, 0);
    ctx.scale(-1, 1);
  }
  ctx.drawImage(v, 0, 0);
  const photoData = c.toDataURL("image/jpeg", 0.82);
  executeKioskCheckin(photoData);
}

function skipKioskPhoto() {
  executeKioskCheckin("");
}

function cancelKioskCheckin() {
  closeKioskCamera();
}

async function executeKioskCheckin(photoData) {
  if (!kioskPendingCheckin.value) return;
  const { cardId, payload, message, details, isBindMode } = kioskPendingCheckin.value;
  
  try {
    const finalPayload = { ...payload, photo: photoData || "" };
    const checkinRes = await fetch('/api/records/checkin-card', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(finalPayload)
    }).then(r => r.json());

    if (checkinRes.success) {
      kioskStatus.value = {
        active: true,
        success: true,
        message: message,
        details: details,
        returnCardAlert: false
      };
      if (isBindMode) {
        waitingForTempCard.value = false;
        showKioskRegister.value = false;
      }
      refreshData();
      playBeep(true);
    } else {
      kioskStatus.value = {
        active: true,
        success: false,
        message: t.value.kioskSwipeError,
        details: checkinRes.error || "進場登記失敗",
        returnCardAlert: false
      };
      playBeep(false);
    }
  } catch (e) {
    console.error("Kiosk Swipe Checkin Error:", e);
    kioskStatus.value = {
      active: true,
      success: false,
      message: t.value.kioskSystemError || "系統連線錯誤",
      details: `${e.toString()} | ${t.value.kioskSystemErrorSub || "請檢查後端伺服器與網路。"}`,
      returnCardAlert: false
    };
    playBeep(false);
  } finally {
    closeKioskCamera();
  }
}

function playBeep(success) {
  try {
    const ctx = new (window.AudioContext || window.webkitAudioContext)();
    const osc = ctx.createOscillator();
    const gain = ctx.createGain();
    osc.connect(gain);
    gain.connect(ctx.destination);
    if (success) {
      osc.frequency.setValueAtTime(880, ctx.currentTime);
      gain.gain.setValueAtTime(0.08, ctx.currentTime);
      osc.start();
      osc.stop(ctx.currentTime + 0.12);
    } else {
      osc.frequency.setValueAtTime(220, ctx.currentTime);
      gain.gain.setValueAtTime(0.12, ctx.currentTime);
      osc.start();
      osc.stop(ctx.currentTime + 0.3);
    }
  } catch (e) {}
}

function openKiosk() {
  showKiosk.value = true;
  showKioskRegister.value = false;
  waitingForTempCard.value = false;
  kioskStatus.value = { active: false, success: true, message: '', details: '', returnCardAlert: false };
  cardInput.value = '';
  kioskForm.name = '';
  kioskForm.company = '';
  kioskForm.type = 'vendor';
  kioskForm.department = '';
  kioskForm.contact = defaultContact.value.name;
  kioskForm.purpose = purposes.value[0] || '業務洽談';
  kioskForm.note = '';
  
  setTimeout(() => {
    const el = document.getElementById('kiosk-card-focus');
    if (el) el.focus();
  }, 120);
}

function handleKioskSimSwipe(cardId) {
  cardInput.value = cardId;
  handleCardSwipe();
}

async function handleCardSwipe() {
  const cardId = cardInput.value.trim().toUpperCase();
  cardInput.value = '';
  if (!cardId) return;

  if (waitingForTempCard.value) {
    // We are binding a new temporary card!
    await bindTempCard(cardId);
    return;
  }

  try {
    const res = await fetch(`/api/records/by-card/${cardId}`).then(r => r.json());
    
    if (res.success && res.found) {
      if (res.type === 'active_record') {
        const checkoutRes = await fetch(`/api/records/checkout-card/${cardId}`, { method: 'POST' }).then(r => r.json());
        if (checkoutRes.success) {
          const record = checkoutRes.data;
          kioskStatus.value = {
            active: true,
            success: true,
            message: `${record.name} ${t.value.kioskSwipeCheckout}`,
            details: `${t.value.kioskSwipeExitTime}: ${formatTime(checkoutRes.exitTime)} | ${t.value.kioskSwipeDuration}: ${formatDuration(checkoutRes.exitTime - record.entryTime)}`,
            returnCardAlert: cardId.startsWith('TEMP')
          };
          refreshData();
          playBeep(true);
        }
      } else if (res.type === 'employee') {
        const employee = res.data;
        openKioskCamera({
          cardId: cardId,
          payload: {
            name: employee.name,
            company: '本廠員工',
            type: 'staff',
            department: employee.dept,
            contact: employee.name,
            purpose: '內部員工進出',
            cardId: cardId,
            empAvatar: employee.avatar
          },
          message: `${t.value.kioskSwipeWelcome} ${t.value.types.staff} ${employee.name}`,
          details: `${t.value.empId}: ${employee.id} | ${t.value.dept}: ${employee.dept} | ${t.value.kioskSwipeTime}: ${formatTime(Date.now())}`
        });
      }
    } else {
      if (cardId.startsWith('VISIT')) {
        const demoName = cardId === 'VISIT-002' ? 'Nguyễn Văn An' : '預約來訪廠商';
        const demoCompany = cardId === 'VISIT-002' ? 'SGS Taiwan' : '大同五金';
        openKioskCamera({
          cardId: cardId,
          payload: {
            name: demoName,
            company: demoCompany,
            type: cardId === 'VISIT-002' ? 'audit' : 'vendor',
            department: '倉儲部',
            contact: defaultContact.value.name,
            purpose: '業務洽談',
            cardId: cardId,
            empAvatar: cardId === 'VISIT-002' ? 'https://api.dicebear.com/7.x/thumbs/svg?seed=E006&backgroundColor=1e293b&shapeColor=fcd34d' : ''
          },
          message: `${t.value.kioskSwipeWelcome} ${t.value.types[cardId === 'VISIT-002' ? 'audit' : 'vendor']} ${demoName}`,
          details: `${t.value.cols[0]}: ${cardId} | ${t.value.company}: ${demoCompany} | ${t.value.kioskSwipeTime}: ${formatTime(Date.now())}`
        });
      } else {
        kioskStatus.value = {
          active: true,
          success: false,
          message: t.value.kioskSwipeError,
          details: `${t.value.cols[0]}: ${cardId} | ${t.value.kioskSwipeErrorSub}`,
          returnCardAlert: false
        };
        playBeep(false);
      }
    }
  } catch (e) {
    console.error("Kiosk Swipe Error:", e);
    kioskStatus.value = {
      active: true,
      success: false,
      message: t.value.kioskSystemError || "系統連線錯誤",
      details: `${e.toString()} | ${t.value.kioskSystemErrorSub || "請檢查後端伺服器與網路。"}`,
      returnCardAlert: false
    };
    playBeep(false);
  }

  setTimeout(() => {
    const el = document.getElementById('kiosk-card-focus');
    if (el) el.focus();
  }, 100);
}

function startTempCardBinding() {
  if (!kioskForm.name.trim()) {
    alert(t.value.kioskAlertName);
    return;
  }
  if (kioskForm.type === 'staff' && !kioskForm.company.trim()) {
    alert(t.value.kioskAlertCompany);
    return;
  }
  waitingForTempCard.value = true;
  setTimeout(() => {
    const el = document.getElementById('kiosk-card-focus');
    if (el) el.focus();
  }, 100);
}

async function bindTempCard(cardId) {
  openKioskCamera({
    cardId: cardId,
    payload: {
      name: kioskForm.name,
      company: kioskForm.type === 'staff' ? kioskForm.company : (kioskForm.company || '無'),
      type: kioskForm.type,
      department: kioskForm.type === 'vendor' ? '' : kioskForm.department,
      contact: defaultContact.value.name,
      purpose: kioskForm.purpose,
      note: kioskForm.note,
      cardId: cardId
    },
    message: t.value.kioskAlertBindSuccess,
    details: `${t.value.cols[0]}: ${cardId} | ${t.value.types[kioskForm.type]}: ${kioskForm.name} (${kioskForm.company}) | ${t.value.kioskSwipeTime}: ${formatTime(Date.now())}`,
    isBindMode: true
  });
}

const handleGlobalClick = () => {
  if (showKiosk.value && !showKioskRegister.value && !waitingForTempCard.value) {
    const el = document.getElementById('kiosk-card-focus');
    if (el) el.focus();
  }
};

const handleGlobalKeydown = (e) => {
  if (!showKiosk.value) return;
  
  // If typing in the registration form inputs, do not intercept
  if (showKioskRegister.value && !waitingForTempCard.value) return;
  
  // Do not intercept if focus is explicitly on other interactive form fields
  if (document.activeElement && (document.activeElement.tagName === 'INPUT' || document.activeElement.tagName === 'SELECT' || document.activeElement.tagName === 'TEXTAREA')) {
    if (document.activeElement.id !== 'kiosk-card-focus') {
      return;
    }
  }

  const el = document.getElementById('kiosk-card-focus');
  if (el && document.activeElement !== el) {
    el.focus();
  }
};

onMounted(() => {
  refreshData();
  window.addEventListener('click', handleGlobalClick);
  window.addEventListener('keydown', handleGlobalKeydown);
});

onUnmounted(() => {
  window.removeEventListener('click', handleGlobalClick);
  window.removeEventListener('keydown', handleGlobalKeydown);
});
</script>

<template>
  <div class="app-container">
    <!-- Header Banner -->
    <header class="app-header">
      <div class="app-header-inner">
        <div class="logo-wrapper">
          <!-- Transparent background brand logo using official logo image -->
          <img class="header-logo-img" src="/logo.png" alt="JIA HSIN" />
          <div class="logo-divider"></div>
          <div class="logo-text">
            <h1>{{ t.sysTitle }}</h1>
            <p>{{ t.sysSub }}</p>
          </div>
        </div>
        <!-- Controls -->
        <div class="header-controls">
          <div class="lang-pills">
            <button :class="['lang-pill', { active: lang === 'zh' }]" @click="lang = 'zh'">中文</button>
            <button :class="['lang-pill', { active: lang === 'en' }]" @click="lang = 'en'">EN</button>
            <button :class="['lang-pill', { active: lang === 'vi' }]" @click="lang = 'vi'">VI</button>
          </div>
          <button class="btn-kiosk-header" @click="openKiosk">
            <span class="kiosk-icon">💻</span> 自助感應登記
          </button>
          <button class="btn-settings-header" @click="showSettings = true">
            <span class="settings-icon">⚙</span> {{ t.settings }}
          </button>
          <button class="btn-primary-header" @click="showRegister = true">{{ t.register }}</button>
        </div>
      </div>
    </header>

    <main class="app-content">
      <!-- Contact Info Bar -->
      <div class="contact-info-bar">
        <span class="pulse-dot"></span>
        <span class="contact-label">接洽人員：<strong>{{ defaultContact.name }}</strong> · {{ defaultContact.dept }}</span>
      </div>

      <!-- Core Statistics Cards Grid -->
      <section class="stats-grid">
        <div class="stat-card clickable" @click="activeTab = 'monitor'">
          <span class="stat-value-large color-onsite">{{ stats.onSite }}</span>
          <span class="stat-label-bottom">{{ t.statsInside }}</span>
        </div>
        <div class="stat-card clickable" @click="showTodayVisitors = true">
          <span class="stat-value-large color-today">{{ stats.today }}</span>
          <span class="stat-label-bottom">{{ t.statsToday }}</span>
        </div>
      </section>

      <!-- Navigation Tabs -->
      <nav class="nav-tabs">
        <button :class="['tab-btn', { active: activeTab === 'monitor' }]" @click="activeTab = 'monitor'">
          👤 {{ t.monitor }}
        </button>
        <button :class="['tab-btn', { active: activeTab === 'history' }]" @click="activeTab = 'history'">
          📋 {{ t.history }}
        </button>
      </nav>

      <!-- SEPARATED Search & Filters (Rendered separately depending on activeTab) -->
      
      <!-- 1. Live Monitor Filters (NO Export CSV, NO status, NO Date selection) -->
      <div class="filters-bar" v-if="activeTab === 'monitor'">
        <div class="search-input-wrapper">
          <span class="search-icon">🔍</span>
          <input type="text" v-model="monitorSearch" :placeholder="t.searchPlaceholder" />
        </div>
        <div class="filters-group">
          <select v-model="monitorFilterType">
            <option value="">{{ t.allTypes }}</option>
            <option v-for="type in VISITOR_TYPES" :key="type" :value="type">{{ t.types[type] }}</option>
          </select>
        </div>
      </div>

      <!-- 2. History Log Filters (WITH Date range query, status selection, and Export CSV) -->
      <div class="filters-bar history-filters-bar" v-if="activeTab === 'history'">
        <div class="search-input-wrapper">
          <span class="search-icon">🔍</span>
          <input type="text" v-model="historySearchInput" :placeholder="t.searchPlaceholder" />
        </div>
        <div class="filters-group">
          <select v-model="historyFilterTypeInput">
            <option value="">{{ t.allTypes }}</option>
            <option v-for="type in VISITOR_TYPES" :key="type" :value="type">{{ t.types[type] }}</option>
          </select>
          <select v-model="filterStatusInput">
            <option value="">{{ t.allStatus }}</option>
            <option value="inside">{{ t.inside }}</option>
            <option value="exited">{{ t.exited }}</option>
          </select>

          <!-- Date Range Selector with Manual Query Trigger Button -->
          <div class="date-range-wrapper">
            <input type="date" v-model="filterStartDate" class="date-input" />
            <span class="date-sep">~</span>
            <input type="date" v-model="filterEndDate" class="date-input" />
            <button class="btn-query-history" @click="handleHistoryQuery">查詢</button>
          </div>

          <!-- Export CSV is ONLY shown in History Log tab -->
          <button class="btn-secondary" @click="exportCSV">{{ t.exportCSV }}</button>
        </div>
      </div>

      <!-- Active On-site List Grid (For Monitor tab) -->
      <div class="monitor-panel" v-if="activeTab === 'monitor'">
        <div v-if="filteredActiveVisitors.length === 0" class="empty-state">
          <div class="empty-icon">📂</div>
          <p>{{ t.noVisitors }}</p>
        </div>

        <div v-else class="monitor-cards-grid">
          <div v-for="r in filteredActiveVisitors" :key="r.id" class="visitor-card">
            <!-- Card Upper-Right Badge -->
            <div class="card-header-badge">
              <span class="type-tag" :style="{ backgroundColor: TYPE_COLORS[r.type] + '15', color: TYPE_COLORS[r.type], borderColor: TYPE_COLORS[r.type] + '33' }">
                {{ t.types[r.type] }}
              </span>
            </div>

            <!-- Centered Card Avatar Section -->
            <div class="card-avatar-section" @click="showDetail = r">
              <div class="avatar-circle">
                <img v-if="r.type === 'staff' && r.empAvatar" :src="r.empAvatar" alt="staff" class="avatar-img-staff" />
                <img v-else-if="r.photo" :src="r.photo" alt="visitor" class="avatar-img" />
                <div v-else class="avatar-placeholder-silhouette">
                  <!-- Silhouette SVG -->
                  <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="#94a3b8" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
                    <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" />
                    <circle cx="12" cy="7" r="4" />
                  </svg>
                </div>
              </div>
              <span class="avatar-status-text" v-if="!r.photo && r.type !== 'staff'">未拍照</span>
            </div>

            <!-- Card Body details -->
            <div class="card-body">
              <div class="card-main-row">
                <div class="card-identity">
                  <h3 class="visitor-name" @click="showDetail = r">{{ r.name }}</h3>
                  <p class="visitor-company">{{ r.company }}</p>
                </div>
                <div class="card-time-info">
                  <div class="time-item">
                    🕒 {{ formatSimpleTime(r.entryTime) }}
                  </div>
                  <div class="duration-item" :class="{ 'long-stay': isLongStay(r.entryTime) }">
                    ⏳ {{ getStayDuration(r.entryTime) }}
                  </div>
                </div>
              </div>

              <!-- Card Bottom Footer row -->
              <div class="card-footer-row">
                <div class="card-meta">
                  <span class="purpose-badge">{{ r.purpose }}</span>
                  <span class="contact-badge">
                    👤 {{ r.contact }}
                  </span>
                </div>
                <button class="btn-checkout-card" @click="showCheckout = r">
                  {{ t.checkout }}
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Historical Visitors Grid Table (For History tab) -->
      <div class="dashboard-panel" v-if="activeTab === 'history'">
        <div class="table-responsive">
          <table>
            <thead>
              <tr>
                <th v-for="c in t.cols" :key="c">{{ c }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="filteredRecords.length === 0">
                <td colspan="10" class="table-empty">{{ t.noRecords }}</td>
              </tr>
              <tr v-else v-for="r in filteredRecords" :key="r.id">
                <td><span class="id-tag">{{ r.id }}</span></td>
                <td><strong>{{ r.name }}</strong></td>
                <td>{{ r.company }}</td>
                <td>
                  <span class="type-badge" :style="{ backgroundColor: TYPE_COLORS[r.type] + '1a', color: TYPE_COLORS[r.type] }">
                    {{ t.types[r.type] }}
                  </span>
                </td>
                <td>{{ r.contact }}</td>
                <td>{{ r.purpose }}</td>
                <td class="time-col">{{ formatTime(r.entryTime) }}</td>
                <td class="time-col">
                  <span v-if="!r.exitTime" class="inside-badge">{{ t.stillInside }}</span>
                  <span v-else>{{ formatTime(r.exitTime) }}</span>
                </td>
                <td>{{ r.exitTime ? formatDuration(r.exitTime - r.entryTime) : '—' }}</td>
                <td class="actions-col">
                  <button v-if="!r.exitTime" class="btn-checkout-sm" @click="showCheckout = r">{{ t.checkout }}</button>
                  <button class="btn-info-sm" @click="showDetail = r">{{ t.details }}</button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <div class="table-footer">
          {{ t.total }} <strong>{{ filteredRecords.length }}</strong> {{ t.records }}
        </div>
      </div>
    </main>

    <!-- Floating Global Toasts -->
    <div class="toast-container">
      <div v-for="toast in toasts" :key="toast.id" class="toast-item">
        {{ toast.message }}
      </div>
    </div>

    <!-- Modals Layer Injectors -->
    <RegisterModal 
      v-if="showRegister" 
      :t="t" 
      :departments="departments" 
      :purposes="purposes" 
      :employees="employees"
      :defaultContact="defaultContact"
      @submit="handleEntry" 
      @close="showRegister = false" 
    />

    <CheckoutModal 
      v-if="showCheckout" 
      :t="t" 
      :record="showCheckout" 
      @confirm="handleCheckout" 
      @close="showCheckout = null" 
    />

    <DetailModal 
      v-if="showDetail" 
      :t="t" 
      :record="showDetail" 
      @close="showDetail = null" 
    />

    <TodayVisitorsModal 
      v-if="showTodayVisitors" 
      :t="t" 
      :records="records" 
      @close="showTodayVisitors = false" 
    />

    <SettingsModal 
      v-if="showSettings" 
      :t="t" 
      :contact="defaultContact" 
      :departments="departments"
      :purposes="purposes"
      :employees="employees"
      :auditLogs="auditLogs"
      @refresh="refreshData"
      @close="showSettings = false" 
    />

    <!-- stand-alone kiosk sensor terminal modal -->
    <div class="kiosk-backdrop" v-if="showKiosk">
      <div class="kiosk-container">
        <!-- Close Kiosk Header -->
        <div class="kiosk-header">
          <div class="kiosk-header-left">
            <span class="kiosk-logo-dot"></span>
            <h2>{{ t.kioskTitle }}</h2>
          </div>
          <div class="kiosk-header-right">
            <button class="btn-kiosk-close" @click="showKiosk = false">{{ t.kioskClose }}</button>
          </div>
        </div>

        <div class="kiosk-body">
          <!-- Left side: RFID Card sensor animation -->
          <div class="kiosk-sensor-panel">
            <div class="kiosk-digital-clock">
              {{ formatSimpleTime(Date.now()) }}
            </div>

            <!-- Radar sensor area -->
            <div class="radar-box" :class="{ error: kioskStatus.active && !kioskStatus.success }">
              <div class="radar-circle-1"></div>
              <div class="radar-circle-2"></div>
              <div class="radar-icons-group">
                <span class="radar-icon card-icon">💳</span>
                <span class="radar-icon-divider">/</span>
                <span class="radar-icon barcode-icon">█▄█</span>
              </div>
            </div>

            <p class="radar-hint">{{ t.kioskHint }}</p>

            <!-- Keyboard focus capture field (focused automatically) -->
            <div class="kiosk-hidden-input-box">
              <input 
                id="kiosk-card-focus" 
                type="text" 
                v-model="cardInput" 
                @keydown.enter="handleCardSwipe" 
                placeholder="Scanner input area..."
                class="kiosk-card-input"
              />
              <button class="btn-kiosk-manual-submit" @click="handleCardSwipe">{{ t.confirm }}</button>
            </div>

            <!-- Swipe Feedback Banner -->
            <div v-if="kioskStatus.active" class="kiosk-feedback-banner" :class="{ error: !kioskStatus.success, warning: kioskStatus.returnCardAlert }">
              <div class="feedback-icon">{{ kioskStatus.success ? '✓' : '✗' }}</div>
              <div class="feedback-text">
                <h3>{{ kioskStatus.message }}</h3>
                <p>{{ kioskStatus.details }}</p>
                <div v-if="kioskStatus.returnCardAlert" class="return-alert-blink">
                  {{ t.kioskSwipeReturnCard }}
                </div>
              </div>
            </div>

            <!-- Manual Kiosk Registration trigger button -->
            <div class="kiosk-actions-row" v-if="!showKioskRegister">
              <button class="btn-kiosk-action-temp" @click="showKioskRegister = true">{{ t.kioskGetTemp }}</button>
            </div>
          </div>

          <!-- Right side: Simulator panel + Temporary Card Manual Register form -->
          <div class="kiosk-sidebar-panel">
            <!-- Simulated Kiosk Registration Form -->
            <div class="kiosk-register-form-box" v-if="showKioskRegister">
              <h3>{{ t.kioskBindTitle }}</h3>
              
              <div v-if="!waitingForTempCard" class="kiosk-form-fields">
                <div class="kiosk-form-row">
                  <label>{{ t.stepLabels[0] === '基本資料' ? '人員類型' : (t.stepLabels[0] === 'Basic Info' ? 'Type' : 'Loại') }} (Type)</label>
                  <select v-model="kioskForm.type" @change="handleKioskTypeChange">
                    <option value="vendor">{{ t.types.vendor }} (Vendor)</option>
                    <option value="brand">{{ t.types.brand }} (Brand)</option>
                    <option value="audit">{{ t.types.audit }} (Auditor)</option>
                    <option value="staff">{{ t.types.staff }} (Employee)</option>
                  </select>
                </div>
                <div class="kiosk-form-row">
                  <label>{{ t.name }} (Name) *</label>
                  <input type="text" v-model="kioskForm.name" :placeholder="t.name" />
                </div>
                <div class="kiosk-form-row">
                  <label>{{ t.company }} (Company){{ kioskForm.type === 'staff' ? ' *' : '' }}</label>
                  <input type="text" v-model="kioskForm.company" :placeholder="t.company" />
                </div>
                <div class="kiosk-form-row">
                  <label>{{ t.dept }} (Department){{ kioskForm.type === 'vendor' ? t.kioskDeptVendor : '' }}</label>
                  <select v-model="kioskForm.department" :disabled="kioskForm.type === 'vendor'">
                    <option value="" v-if="kioskForm.type === 'vendor'">—</option>
                    <option v-for="d in departments" :key="d" :value="d" v-else>{{ d }}</option>
                  </select>
                </div>
                <div class="kiosk-form-row">
                  <label>{{ t.purpose }} (Purpose)</label>
                  <select v-model="kioskForm.purpose">
                    <option v-for="p in purposes" :key="p" :value="p">{{ p }}</option>
                  </select>
                </div>
                <div class="kiosk-form-row">
                  <label>{{ t.note }} (Note)</label>
                  <input type="text" v-model="kioskForm.note" :placeholder="t.note" />
                </div>
                
                <div class="kiosk-form-actions">
                  <button class="btn-kiosk-cancel" @click="showKioskRegister = false">{{ t.cancel }}</button>
                  <button class="btn-kiosk-submit-bind" @click="startTempCardBinding">{{ t.kioskSubmit }} →</button>
                </div>
              </div>

              <!-- Waiting for card sweep simulation -->
              <div v-else class="kiosk-waiting-card-overlay">
                <div class="kiosk-spinner-pulse"></div>
                <h4>{{ t.kioskWaitingBind }}</h4>
                <p>{{ t.kioskWaitingHint }}</p>
                <button class="btn-kiosk-cancel" @click="waitingForTempCard = false">{{ t.kioskBack }}</button>
              </div>
            </div>

            <!-- Sensors Simulator Panel (Always visible below overlays to allow click testing) -->
            <div class="kiosk-simulator-box" :style="{ marginTop: showKioskRegister ? '20px' : '0' }">
              <div class="simulator-header">
                <h3>{{ t.kioskSimHeader }}</h3>
                <p>{{ t.kioskSimSub }}</p>
              </div>

              <div class="sim-card-group">
                <h4>{{ t.kioskSimStaff }}</h4>
                <div class="sim-card-row">
                  <div class="sim-card employee" @click="handleKioskSimSwipe('E001')">
                    <div class="sim-card-chip"></div>
                    <span class="sim-card-label">{{ lang === 'zh' ? '張志明' : (lang === 'en' ? 'Jimmy Zhang' : 'Trương Chí Minh') }}</span>
                    <span class="sim-card-number">E001 (IC)</span>
                  </div>
                  <div class="sim-card employee" @click="handleKioskSimSwipe('E002')">
                    <div class="sim-card-chip"></div>
                    <span class="sim-card-label">{{ lang === 'zh' ? '林美玲' : (lang === 'en' ? 'May Lin' : 'Lâm Mỹ Linh') }}</span>
                    <span class="sim-card-number">E002 (IC)</span>
                  </div>
                </div>

                <h4>{{ t.kioskSimVisitor }}</h4>
                <div class="sim-card-row">
                  <div class="sim-card barcode-ticket" @click="handleKioskSimSwipe('VISIT-002')">
                    <div class="sim-card-barcode-lines">
                      <span></span><span></span><span></span><span></span><span></span><span></span><span></span><span></span><span></span><span></span><span></span>
                    </div>
                    <span class="sim-card-label">{{ lang === 'zh' ? '阮先生 (SGS)' : (lang === 'en' ? 'Mr. Nguyen (SGS)' : 'Ông Nguyễn (SGS)') }}</span>
                    <span class="sim-card-number">VISIT-002</span>
                  </div>
                </div>

                <h4>{{ t.kioskSimTemp }}</h4>
                <div class="sim-card-row">
                  <div class="sim-card temp-card" @click="handleKioskSimSwipe('TEMP-888')">
                    <div class="sim-card-chip"></div>
                    <span class="sim-card-label">{{ lang === 'zh' ? '臨時卡 A' : (lang === 'en' ? 'Temp Card A' : 'Thẻ Tạm A') }}</span>
                    <span class="sim-card-number">TEMP-888</span>
                  </div>
                  <div class="sim-card temp-card" @click="handleKioskSimSwipe('TEMP-999')">
                    <div class="sim-card-chip"></div>
                    <span class="sim-card-label">{{ lang === 'zh' ? '臨時卡 B' : (lang === 'en' ? 'Temp Card B' : 'Thẻ Tạm B') }}</span>
                    <span class="sim-card-number">TEMP-999</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Kiosk Camera Overlaid Layer -->
      <div class="kiosk-camera-layer" v-if="kioskShowCam">
        <div class="kiosk-cam-box">
          <div class="kiosk-cam-header">
            <span>📷 {{ t.camLabel }}</span>
            <span class="kiosk-cam-badge" :style="{ backgroundColor: kioskCamFacing === 'user' ? '#3B82F622' : '#f9731622', color: kioskCamFacing === 'user' ? '#93c5fd' : '#fb923c' }">
              {{ kioskCamFacing === 'user' ? t.camFront : t.camBack }}
            </span>
          </div>

          <div v-if="kioskCamError" class="kiosk-cam-error-box">
            <div class="kiosk-error-emoji">📷</div>
            <p>{{ kioskCamError }}</p>
            <div class="kiosk-error-actions">
              <button class="btn-kiosk-submit-bind" @click="skipKioskPhoto">{{ t.skipPhoto }}</button>
              <button class="btn-kiosk-cancel" @click="cancelKioskCheckin">{{ t.cancel }}</button>
            </div>
          </div>

          <div v-else class="kiosk-cam-preview-box">
            <div class="kiosk-video-frame">
              <video ref="kioskVideoRef" autoplay playsinline muted
                :style="{ transform: kioskCamFacing === 'user' ? 'scaleX(-1)' : 'none', opacity: kioskCamSwitching ? 0.3 : 1 }"></video>
              <div class="kiosk-cam-corners" v-for="c in ['top-left','top-right','bottom-left','bottom-right']" :key="c" :class="c"></div>
              <div v-if="kioskCamSwitching" class="kiosk-cam-switching-overlay">🔄 {{ t.camSwitching }}</div>
            </div>
            <canvas ref="kioskCanvasRef" style="display: none;"></canvas>

            <div class="kiosk-cam-controls">
              <button class="btn-kiosk-cancel" @click="cancelKioskCheckin">{{ t.cancel }}</button>
              <button class="btn-kiosk-shoot" :disabled="!kioskCamReady || kioskCamSwitching" @click="shootKioskPhoto">📸 拍照進場</button>
              <button class="btn-kiosk-skip" @click="skipKioskPhoto">{{ t.skipPhoto }}</button>
              <button class="btn-kiosk-flip" v-if="kioskHasMultipleCams" :disabled="kioskCamSwitching" @click="flipKioskCamera">🔄</button>
            </div>

            <div class="kiosk-cam-hint-text">
              {{ kioskCamFacing === 'user' ? t.camHintFront : t.camHintBack }}
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style>
/* Clean Enterprise Light Blue Theme Design System matching the Jia Hsin design exactly */
:root {
  --bg-primary: #f4f6fa;
  --bg-panel: #ffffff;
  --border-light: #e2e8f0;
  --text-main: #1e293b;
  --text-muted: #64748b;
  --color-primary: #0e4391; /* Jia Hsin Royal Blue */
  --color-primary-hover: #1e40af;
  --color-accent: #22c55e;
  --font-family: 'Inter', 'Outfit', system-ui, sans-serif;
}

body {
  margin: 0;
  font-family: var(--font-family);
  background-color: var(--bg-primary);
  color: var(--text-main);
  min-height: 100vh;
}

.app-container {
  width: 100%;
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

/* Full-width Blue Header Banner matching JIA HSIN */
.app-header {
  background-color: var(--color-primary);
  background-image: linear-gradient(135deg, #0a3575 0%, #0e4391 100%);
  color: #ffffff;
  padding: 16px 0;
  border-bottom: 3.5px solid #00df89;
  box-shadow: 0 4px 20px rgba(14, 67, 145, 0.15);
}

.app-header-inner {
  max-width: 1240px;
  margin: 0 auto;
  padding: 0 24px;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.logo-wrapper {
  display: flex;
  align-items: center;
  gap: 16px;
}

.header-logo-img {
  height: 36px;
  width: auto;
  display: block;
}

.logo-divider {
  width: 1.5px;
  height: 36px;
  background-color: rgba(255, 255, 255, 0.25);
}

/* WAREHOUSE ACCESS text sized at 20px */
.logo-text h1 {
  margin: 0;
  font-size: 20px;
  font-weight: 800;
  letter-spacing: 0.5px;
  font-family: 'Outfit', sans-serif;
  line-height: 1.2;
}

.logo-text p {
  margin: 2px 0 0 0;
  font-size: 11px;
  color: rgba(255, 255, 255, 0.65);
  font-weight: 500;
  line-height: 1.2;
}

.header-controls {
  display: flex;
  align-items: center;
  gap: 16px;
}

/* Language Pills Switcher */
.lang-pills {
  display: flex;
  background-color: rgba(255, 255, 255, 0.1);
  padding: 3px;
  border-radius: 8px;
  border: 1px solid rgba(255, 255, 255, 0.15);
}

.lang-pill {
  background: transparent;
  border: none;
  color: rgba(255, 255, 255, 0.8);
  font-size: 12px;
  font-weight: 600;
  padding: 6px 14px;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.2s;
}

.lang-pill.active {
  background-color: #ffffff;
  color: var(--color-primary);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

/* Header Buttons */
.btn-settings-header {
  background-color: rgba(255, 255, 255, 0.08);
  border: 1px solid rgba(255, 255, 255, 0.15);
  color: #ffffff;
  font-size: 13px;
  font-weight: 600;
  padding: 8px 16px;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s;
  display: flex;
  align-items: center;
  gap: 6px;
}
.btn-settings-header:hover {
  background-color: rgba(255, 255, 255, 0.15);
}

.btn-primary-header {
  background-color: #ffffff;
  color: var(--color-primary);
  border: none;
  font-size: 13px;
  font-weight: 700;
  padding: 9px 20px;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s;
  box-shadow: 0 4px 10px rgba(0, 0, 0, 0.1);
}
.btn-primary-header:hover {
  transform: translateY(-1px);
  box-shadow: 0 6px 15px rgba(0, 0, 0, 0.15);
}

/* Main Content Area */
.app-content {
  max-width: 1240px;
  margin: 24px auto;
  padding: 0 24px;
  box-sizing: border-box;
}

/* Pulse dot info bar */
.contact-info-bar {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  background-color: #ecfdf5;
  border: 1px solid #d1fae5;
  border-radius: 20px;
  padding: 6px 16px;
  margin-bottom: 20px;
}

.pulse-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background-color: #10b981;
  display: inline-block;
  position: relative;
}
.pulse-dot::after {
  content: '';
  position: absolute;
  inset: -3px;
  border-radius: 50%;
  border: 1.5px solid #10b981;
  animation: pulse-ring 1.8s infinite;
  opacity: 0;
}
@keyframes pulse-ring {
  0% { transform: scale(1); opacity: 1; }
  100% { transform: scale(2.2); opacity: 0; }
}

.contact-label {
  font-size: 12px;
  color: #065f46;
}

/* Statistics cards */
.stats-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  max-width: 480px;
  gap: 16px;
  margin-bottom: 24px;
}

.stat-card {
  background-color: var(--bg-panel);
  border: 1px solid var(--border-light);
  border-radius: 12px;
  padding: 16px 20px;
  box-shadow: 0 4px 10px rgba(0, 0, 0, 0.02);
  transition: all 0.2s;
  display: flex;
  flex-direction: column;
}
.stat-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 6px 16px rgba(0, 0, 0, 0.05);
}

.stat-value-large {
  font-size: 32px;
  font-weight: 800;
  line-height: 1;
}

.stat-label-bottom {
  margin-top: 8px;
  font-size: 13px;
  color: var(--text-muted);
  font-weight: 600;
}

.stat-card.clickable {
  cursor: pointer;
}

.color-onsite { color: #22c55e; }
.color-today { color: #818cf8; }

/* Navigation Tabs Row */
.nav-tabs {
  display: flex;
  gap: 8px;
  margin-bottom: 20px;
  align-items: center;
}

.tab-btn {
  background-color: #e5e7eb;
  border: none;
  color: #4b5563;
  font-weight: 700;
  font-size: 13px;
  padding: 10px 20px;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s;
  display: flex;
  align-items: center;
  gap: 6px;
}
.tab-btn:hover {
  background-color: #d1d5db;
  color: #1f2937;
}

.tab-btn.active {
  background-color: var(--color-primary);
  color: #ffffff;
  box-shadow: 0 4px 12px rgba(14, 67, 145, 0.25);
}

/* Filters and Search Row */
.filters-bar {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 24px;
  background-color: var(--bg-panel);
  border: 1px solid var(--border-light);
  padding: 12px 20px;
  border-radius: 12px;
  box-shadow: 0 4px 10px rgba(0, 0, 0, 0.02);
}

@media (max-width: 1024px) {
  .history-filters-bar {
    flex-direction: column;
    align-items: stretch;
  }
  .history-filters-bar .filters-group {
    flex-wrap: wrap;
  }
}

.search-input-wrapper {
  position: relative;
  flex: 1;
}

.search-icon {
  position: absolute;
  left: 14px;
  top: 50%;
  transform: translateY(-50%);
  color: var(--text-muted);
  font-size: 14px;
}

.search-input-wrapper input {
  width: 100%;
  background-color: #f9fafb;
  border: 1px solid var(--border-light);
  border-radius: 8px;
  padding: 10px 16px 10px 40px;
  color: var(--text-main);
  outline: none;
  font-size: 13px;
  box-sizing: border-box;
  transition: border-color 0.2s;
}
.search-input-wrapper input:focus {
  border-color: var(--color-primary);
  background-color: #ffffff;
}

.filters-group {
  display: flex;
  gap: 10px;
  align-items: center;
}

.filters-group select {
  background-color: #f9fafb;
  border: 1px solid var(--border-light);
  color: var(--text-main);
  padding: 9px 16px;
  border-radius: 8px;
  outline: none;
  cursor: pointer;
  font-size: 13px;
  transition: border-color 0.2s;
}
.filters-group select:focus {
  border-color: var(--color-primary);
}

/* Date Range Selector Widget styling */
.date-range-wrapper {
  display: flex;
  align-items: center;
  gap: 8px;
  background-color: #f9fafb;
  border: 1px solid var(--border-light);
  padding: 4px 10px;
  border-radius: 8px;
}

.date-input {
  border: none;
  background: transparent;
  font-size: 13px;
  color: var(--text-main);
  outline: none;
  cursor: pointer;
  font-family: inherit;
}

.date-sep {
  font-size: 13px;
  color: var(--text-muted);
}

.btn-query-history {
  background-color: var(--color-primary);
  color: #ffffff;
  border: none;
  border-radius: 6px;
  padding: 6px 16px;
  font-size: 13px;
  font-weight: 700;
  cursor: pointer;
  transition: background-color 0.2s;
}
.btn-query-history:hover {
  background-color: var(--color-primary-hover);
}

.btn-secondary {
  background-color: #ffffff;
  border: 1px solid var(--border-light);
  color: var(--text-main);
  padding: 9px 18px;
  border-radius: 8px;
  cursor: pointer;
  font-size: 13px;
  font-weight: 600;
  transition: all 0.2s;
}
.btn-secondary:hover {
  background-color: #f9fafb;
  border-color: #cbd5e1;
}

/* Visitor Cards Grid (Monitor Tab) */
.monitor-cards-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(360px, 1fr));
  gap: 24px;
}

@media (max-width: 640px) {
  .monitor-cards-grid {
    grid-template-columns: 1fr;
  }
}

.visitor-card {
  background-color: var(--bg-panel);
  border: 1px solid var(--border-light);
  border-radius: 16px;
  padding: 20px;
  position: relative;
  display: flex;
  flex-direction: column;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.02);
  transition: all 0.25s;
}
.visitor-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.06);
  border-color: #cbd5e1;
}

/* Card Header Badge */
.card-header-badge {
  position: absolute;
  top: 16px;
  right: 16px;
}

.type-tag {
  display: inline-block;
  font-size: 10px;
  padding: 3px 8px;
  border-radius: 6px;
  font-weight: 700;
  border: 1px solid transparent;
}

/* Centered Avatar Section */
.card-avatar-section {
  display: flex;
  flex-direction: column;
  align-items: center;
  margin-bottom: 16px;
  cursor: pointer;
}

.avatar-circle {
  width: 64px;
  height: 64px;
  border-radius: 50%;
  overflow: hidden;
  background-color: #f3f4f6;
  border: 2px solid #e5e7eb;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: transform 0.2s;
}
.avatar-circle:hover {
  transform: scale(1.05);
}

.avatar-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.avatar-img-staff {
  width: 80%;
  height: 80%;
  object-fit: contain;
}

.avatar-placeholder-silhouette {
  display: flex;
  align-items: center;
  justify-content: center;
}

.avatar-status-text {
  margin-top: 6px;
  font-size: 10px;
  color: var(--text-muted);
  font-weight: 700;
}

/* Card Body Details */
.card-body {
  display: flex;
  flex-direction: column;
  flex: 1;
}

.card-main-row {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 16px;
  border-bottom: 1px solid #f1f5f9;
  padding-bottom: 12px;
}

.visitor-name {
  margin: 0;
  font-size: 18px;
  font-weight: 800;
  color: #0f172a;
  cursor: pointer;
  transition: color 0.2s;
}
.visitor-name:hover {
  color: var(--color-primary);
}

.visitor-company {
  margin: 2px 0 0 0;
  font-size: 13px;
  color: var(--text-muted);
  font-weight: 500;
}

.card-time-info {
  text-align: right;
}

.time-item {
  font-size: 12px;
  color: var(--text-muted);
  font-weight: 500;
}

.duration-item {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  margin-top: 4px;
  font-size: 11px;
  font-weight: 700;
  background-color: #fef3c7;
  color: #d97706;
  padding: 2px 8px;
  border-radius: 12px;
}

.duration-item.long-stay {
  background-color: #fee2e2;
  color: #dc2626;
}

/* Card Footer Row */
.card-footer-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-top: auto;
}

.card-meta {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.purpose-badge {
  font-size: 11px;
  font-weight: 700;
  background-color: #f1f5f9;
  color: #475569;
  padding: 2px 8px;
  border-radius: 6px;
  align-self: flex-start;
}

.contact-badge {
  font-size: 11px;
  color: var(--text-muted);
  font-weight: 500;
  display: flex;
  align-items: center;
  gap: 4px;
}

/* Pink Outline Checkout Button */
.btn-checkout-card {
  background-color: #fef2f2;
  border: 1.5px solid #fee2e2;
  color: #dc2626;
  font-size: 12px;
  font-weight: 700;
  padding: 6px 14px;
  border-radius: 20px;
  cursor: pointer;
  transition: all 0.2s;
}
.btn-checkout-card:hover {
  background-color: #fee2e2;
  border-color: #fca5a5;
  box-shadow: 0 2px 8px rgba(220, 38, 38, 0.1);
}

/* History Grid Panel Table */
.dashboard-panel {
  background-color: var(--bg-panel);
  border: 1px solid var(--border-light);
  border-radius: 16px;
  padding: 24px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.02);
  margin-bottom: 24px;
}

.panel-header h2 {
  margin: 0 0 20px 0;
  font-size: 16px;
  font-weight: 800;
  color: #0f172a;
}

/* Empty State */
.empty-state {
  text-align: center;
  padding: 64px 20px;
  color: var(--text-muted);
}

.empty-icon {
  font-size: 48px;
  margin-bottom: 12px;
}

/* Data Table Styling */
.table-responsive {
  width: 100%;
  overflow-x: auto;
}

table {
  width: 100%;
  border-collapse: collapse;
  text-align: left;
  font-size: 13px;
}

th {
  color: var(--text-muted);
  font-weight: 700;
  padding: 12px 16px;
  border-bottom: 2px solid var(--border-light);
  font-size: 11px;
  letter-spacing: 0.5px;
  text-transform: uppercase;
}

td {
  padding: 14px 16px;
  border-bottom: 1px solid #f1f5f9;
  color: var(--text-main);
  vertical-align: middle;
}

tr:hover td {
  background-color: #f8fafc;
}

.table-empty {
  text-align: center;
  color: var(--text-muted);
  padding: 48px !important;
}

.id-tag {
  background-color: #f1f5f9;
  color: #475569;
  padding: 2px 6px;
  border-radius: 4px;
  font-weight: 700;
  font-size: 10px;
}

.time-col {
  color: var(--text-muted);
  font-size: 12px;
}

.inside-badge {
  background-color: #fef3c7;
  color: #d97706;
  padding: 2px 8px;
  border-radius: 6px;
  font-weight: 700;
  font-size: 11px;
}

.actions-col {
  display: flex;
  gap: 8px;
}

.btn-checkout-sm {
  background-color: #fef2f2;
  border: 1px solid #fee2e2;
  color: #dc2626;
  font-size: 11px;
  padding: 4px 10px;
  border-radius: 6px;
  cursor: pointer;
  font-weight: 700;
  transition: all 0.2s;
}
.btn-checkout-sm:hover {
  background-color: #fee2e2;
}

.btn-info-sm {
  background-color: #f1f5f9;
  border: 1px solid #e2e8f0;
  color: #334155;
  font-size: 11px;
  padding: 3px 10px;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.2s;
}
.btn-info-sm:hover {
  background-color: #e2e8f0;
}

.table-footer {
  margin-top: 16px;
  font-size: 12px;
  color: var(--text-muted);
  text-align: right;
}

/* Floating Toasts */
.toast-container {
  position: fixed;
  bottom: 24px;
  right: 24px;
  z-index: 10000;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.toast-item {
  background-color: #10b981;
  color: #ffffff;
  padding: 12px 24px;
  border-radius: 8px;
  box-shadow: 0 8px 30px rgba(16, 185, 129, 0.25);
  font-size: 13px;
  font-weight: 700;
  animation: slideIn 0.3s cubic-bezier(0.16, 1, 0.3, 1);
}

@keyframes slideIn {
  from { transform: translateY(20px); opacity: 0; }
  to { transform: translateY(0); opacity: 1; }
}

/* 自助登記終端 Kiosk Terminal Styling */
.btn-kiosk-header {
  background-color: rgba(255, 255, 255, 0.08);
  border: 1px solid rgba(255, 255, 255, 0.15);
  color: #ffffff;
  font-size: 13px;
  font-weight: 600;
  padding: 8px 16px;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s;
  display: flex;
  align-items: center;
  gap: 6px;
}
.btn-kiosk-header:hover {
  background-color: rgba(0, 223, 137, 0.15);
  border-color: #00df89;
  box-shadow: 0 0 10px rgba(0, 223, 137, 0.2);
}

.kiosk-backdrop {
  position: fixed;
  inset: 0;
  background: radial-gradient(circle at center, #1b2640 0%, #0b0f19 100%);
  z-index: 10001;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 24px;
  font-family: var(--font-family);
  color: #f1f5f9;
}

.kiosk-container {
  width: 100%;
  max-width: 1120px;
  height: 90vh;
  background: rgba(30, 41, 59, 0.5);
  backdrop-filter: blur(20px);
  border: 1.5px solid rgba(255, 255, 255, 0.08);
  border-radius: 28px;
  box-shadow: 0 30px 60px rgba(0, 0, 0, 0.5);
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.kiosk-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 20px 32px;
  background: rgba(15, 23, 42, 0.6);
  border-bottom: 1px solid rgba(255, 255, 255, 0.08);
}

.kiosk-header-left {
  display: flex;
  align-items: center;
  gap: 12px;
}

.kiosk-logo-dot {
  width: 12px;
  height: 12px;
  border-radius: 50%;
  background-color: #00df89;
  box-shadow: 0 0 12px #00df89;
  animation: pulse-logo 1.5s infinite;
}
@keyframes pulse-logo {
  0% { transform: scale(1); opacity: 1; }
  50% { transform: scale(1.3); opacity: 0.7; }
  100% { transform: scale(1); opacity: 1; }
}

.kiosk-header h2 {
  margin: 0;
  font-size: 18px;
  font-weight: 800;
  letter-spacing: 0.5px;
  font-family: 'Outfit', sans-serif;
  color: #ffffff;
}

.btn-kiosk-close {
  background: rgba(239, 68, 68, 0.1);
  border: 1px solid rgba(239, 68, 68, 0.3);
  color: #fca5a5;
  padding: 8px 18px;
  border-radius: 8px;
  font-size: 13px;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s;
}
.btn-kiosk-close:hover {
  background: #ef4444;
  color: #ffffff;
  border-color: #ef4444;
}

.kiosk-body {
  display: flex;
  flex: 1;
  overflow: hidden;
}

/* Left sensor panel styling */
.kiosk-sensor-panel {
  flex: 1.1;
  border-right: 1px solid rgba(255, 255, 255, 0.08);
  padding: 40px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  background: rgba(15, 23, 42, 0.2);
  position: relative;
}

.kiosk-digital-clock {
  font-family: 'Outfit', monospace;
  font-size: 24px;
  font-weight: 700;
  color: #94a3b8;
  letter-spacing: 1px;
  position: absolute;
  top: 24px;
}

/* Radar Wave sensor */
.radar-box {
  width: 140px;
  height: 140px;
  border-radius: 50%;
  border: 2px solid rgba(0, 223, 137, 0.4);
  display: flex;
  align-items: center;
  justify-content: center;
  position: relative;
  margin-bottom: 24px;
  transition: all 0.3s;
}

.radar-box.error {
  border-color: rgba(239, 68, 68, 0.4);
}

.radar-circle-1, .radar-circle-2 {
  position: absolute;
  inset: -2px;
  border-radius: 50%;
  border: 2px solid #00df89;
  animation: radar-wave 2.2s cubic-bezier(0.1, 0.8, 0.3, 1) infinite;
  opacity: 0;
}
.radar-box.error .radar-circle-1, .radar-box.error .radar-circle-2 {
  border-color: #ef4444;
}

.radar-circle-2 {
  animation-delay: 1.1s;
}

@keyframes radar-wave {
  0% { transform: scale(1); opacity: 0.8; }
  100% { transform: scale(1.8); opacity: 0; }
}

.radar-icon {
  font-size: 48px;
}

.radar-hint {
  font-size: 15px;
  color: #94a3b8;
  font-weight: 500;
  margin: 0 0 24px 0;
}

.kiosk-hidden-input-box {
  display: flex;
  gap: 8px;
  margin-bottom: 24px;
  width: 100%;
  max-width: 360px;
}

.kiosk-card-input {
  flex: 1;
  background: rgba(15, 23, 42, 0.6);
  border: 1px solid rgba(255, 255, 255, 0.15);
  border-radius: 8px;
  padding: 10px 16px;
  color: #ffffff;
  outline: none;
  font-size: 13px;
  font-family: inherit;
  text-align: center;
}
.kiosk-card-input:focus {
  border-color: #00df89;
  box-shadow: 0 0 10px rgba(0, 223, 137, 0.25);
}

.btn-kiosk-manual-submit {
  background: rgba(255, 255, 255, 0.08);
  border: 1px solid rgba(255, 255, 255, 0.12);
  color: #e2e8f0;
  border-radius: 8px;
  padding: 0 16px;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
}
.btn-kiosk-manual-submit:hover {
  background: rgba(255, 255, 255, 0.15);
}

/* Feedback Banner styling */
.kiosk-feedback-banner {
  width: 100%;
  max-width: 440px;
  background: rgba(16, 185, 129, 0.1);
  border: 1.5px solid rgba(16, 185, 129, 0.3);
  border-radius: 16px;
  padding: 16px 20px;
  display: flex;
  gap: 16px;
  align-items: center;
  animation: kiosk-pop 0.3s cubic-bezier(0.16, 1, 0.3, 1);
  margin-bottom: 24px;
}

.kiosk-feedback-banner.error {
  background: rgba(239, 68, 68, 0.1);
  border-color: rgba(239, 68, 68, 0.3);
}

.kiosk-feedback-banner.warning {
  background: rgba(245, 158, 11, 0.12);
  border-color: rgba(245, 158, 11, 0.4);
  animation: warning-pulse-bg 2s infinite;
}
@keyframes warning-pulse-bg {
  0% { box-shadow: 0 0 0px rgba(245, 158, 11, 0); }
  50% { box-shadow: 0 0 20px rgba(245, 158, 11, 0.25); border-color: rgba(245, 158, 11, 0.7); }
  100% { box-shadow: 0 0 0px rgba(245, 158, 11, 0); }
}

@keyframes kiosk-pop {
  from { transform: scale(0.95); opacity: 0; }
  to { transform: scale(1); opacity: 1; }
}

.feedback-icon {
  width: 38px;
  height: 38px;
  border-radius: 50%;
  background: #10b981;
  color: #ffffff;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
  font-weight: 900;
  flex-shrink: 0;
}
.kiosk-feedback-banner.error .feedback-icon {
  background: #ef4444;
}
.kiosk-feedback-banner.warning .feedback-icon {
  background: #f59e0b;
}

.feedback-text {
  flex: 1;
}

.feedback-text h3 {
  margin: 0;
  font-size: 15px;
  font-weight: 800;
  color: #ffffff;
}

.feedback-text p {
  margin: 4px 0 0 0;
  font-size: 12px;
  color: #94a3b8;
  font-weight: 500;
  line-height: 1.4;
}

.return-alert-blink {
  margin-top: 8px;
  font-size: 12px;
  color: #fbbf24;
  font-weight: 800;
  letter-spacing: 0.5px;
  animation: return-blink 1.2s infinite;
}
@keyframes return-blink {
  0% { opacity: 1; }
  50% { opacity: 0.4; }
  100% { opacity: 1; }
}

.btn-kiosk-action-temp {
  background: #0e4391;
  border: 1.5px solid #1e3a8a;
  color: #ffffff;
  border-radius: 24px;
  padding: 10px 24px;
  font-size: 13px;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s;
  box-shadow: 0 4px 12px rgba(14, 67, 145, 0.3);
}
.btn-kiosk-action-temp:hover {
  background: #1d4ed8;
  transform: translateY(-1px);
  box-shadow: 0 6px 16px rgba(29, 78, 216, 0.4);
}

/* Sidebar panel styling */
.kiosk-sidebar-panel {
  flex: 0.9;
  padding: 40px;
  overflow-y: auto;
}

/* Kiosk Register form styling */
.kiosk-register-form-box {
  background: rgba(15, 23, 42, 0.35);
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 18px;
  padding: 24px;
  box-sizing: border-box;
}

.kiosk-register-form-box h3 {
  margin: 0 0 20px 0;
  font-size: 15px;
  font-weight: 800;
  color: #ffffff;
  border-bottom: 1.5px solid rgba(255, 255, 255, 0.08);
  padding-bottom: 12px;
}

.kiosk-form-fields {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.kiosk-form-row {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.kiosk-form-row label {
  font-size: 11px;
  color: #94a3b8;
  font-weight: 600;
}

.kiosk-form-row input, .kiosk-form-row select {
  background: rgba(15, 23, 42, 0.6);
  border: 1px solid rgba(255, 255, 255, 0.12);
  border-radius: 8px;
  padding: 9px 12px;
  color: #ffffff;
  font-size: 12px;
  outline: none;
  font-family: inherit;
  box-sizing: border-box;
}

.kiosk-form-row select:disabled {
  background: rgba(15, 23, 42, 0.25) !important;
  color: #64748b !important;
  border-color: rgba(255, 255, 255, 0.05) !important;
  cursor: not-allowed;
}
.kiosk-form-row input:focus, .kiosk-form-row select:focus {
  border-color: #00df89;
}

.kiosk-form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  margin-top: 10px;
}

.btn-kiosk-cancel {
  background: transparent;
  border: 1px solid rgba(255, 255, 255, 0.15);
  color: #94a3b8;
  border-radius: 8px;
  padding: 8px 16px;
  font-size: 12px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
}
.btn-kiosk-cancel:hover {
  background: rgba(255, 255, 255, 0.05);
  color: #f1f5f9;
}

.btn-kiosk-submit-bind {
  background: #00df89;
  border: none;
  color: #0a2d5a;
  border-radius: 8px;
  padding: 9px 18px;
  font-size: 12px;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s;
}
.btn-kiosk-submit-bind:hover {
  background: #05ff9f;
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(5, 255, 159, 0.35);
}

.kiosk-waiting-card-overlay {
  text-align: center;
  padding: 40px 10px;
}

.kiosk-spinner-pulse {
  width: 54px;
  height: 54px;
  border-radius: 50%;
  border: 3.5px solid rgba(0, 223, 137, 0.15);
  border-top-color: #00df89;
  animation: spin 1s linear infinite;
  display: inline-block;
  margin-bottom: 20px;
}
@keyframes spin {
  to { transform: rotate(360deg); }
}

.kiosk-waiting-card-overlay h4 {
  margin: 0;
  font-size: 15px;
  font-weight: 800;
  color: #00df89;
  letter-spacing: 0.5px;
}
.kiosk-waiting-card-overlay p {
  margin: 8px 0 24px 0;
  font-size: 12px;
  color: #94a3b8;
  line-height: 1.4;
}

/* Sensors Simulator box styling */
.kiosk-simulator-box {
  background: rgba(15, 23, 42, 0.35);
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 18px;
  padding: 24px;
  box-sizing: border-box;
}

.simulator-header h3 {
  margin: 0;
  font-size: 15px;
  font-weight: 800;
  color: #ffffff;
}
.simulator-header p {
  margin: 6px 0 0 0;
  font-size: 11px;
  color: #94a3b8;
  line-height: 1.4;
}

.sim-card-group {
  margin-top: 24px;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.sim-card-group h4 {
  margin: 0;
  font-size: 11px;
  color: #cbd5e1;
  font-weight: 700;
  letter-spacing: 0.5px;
}

.sim-card-row {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 12px;
}

/* Beautiful simulated cards */
.sim-card {
  height: 68px;
  border-radius: 10px;
  padding: 10px 14px;
  box-sizing: border-box;
  position: relative;
  cursor: pointer;
  transition: all 0.2s;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  box-shadow: 0 4px 10px rgba(0, 0, 0, 0.2);
}
.sim-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 8px 18px rgba(0, 0, 0, 0.35);
}

.sim-card.employee {
  background: linear-gradient(135deg, #1e3a8a 0%, #0d1b3e 100%);
  border: 1px solid rgba(59, 130, 246, 0.3);
}
.sim-card.employee:hover {
  border-color: #3b82f6;
}

.sim-card.visitor {
  background: linear-gradient(135deg, #0f172a 0%, #1e293b 100%);
  border: 1px solid rgba(148, 163, 184, 0.3);
}
.sim-card.visitor:hover {
  border-color: #94a3b8;
}

.sim-card.temp-card {
  background: linear-gradient(135deg, #065f46 0%, #022c22 100%);
  border: 1px solid rgba(16, 185, 129, 0.3);
}
.sim-card.temp-card:hover {
  border-color: #10b981;
}

.sim-card-chip {
  width: 14px;
  height: 11px;
  background: radial-gradient(circle at center, #ffd700 0%, #d4af37 100%);
  border-radius: 2px;
}

.sim-card-label {
  font-size: 11px;
  font-weight: 700;
  color: #ffffff;
}

.sim-card-number {
  font-family: monospace;
  font-size: 10px;
  color: rgba(255, 255, 255, 0.5);
  position: absolute;
  top: 10px;
  right: 12px;
}

/* Barcode & IC Card UI Enhancements */
.radar-icons-group {
  display: flex;
  align-items: center;
  gap: 12px;
}

.radar-icon-divider {
  font-size: 28px;
  color: rgba(255, 255, 255, 0.25);
  font-weight: 300;
}

.sim-card.barcode-ticket {
  background: linear-gradient(135deg, #ffffff 0%, #f1f5f9 100%);
  border: 1.5px dashed #cbd5e1;
  color: #0f172a;
}
.sim-card.barcode-ticket:hover {
  border-color: #64748b;
  box-shadow: 0 8px 18px rgba(255, 255, 255, 0.15);
}

.sim-card.barcode-ticket .sim-card-label {
  color: #0f172a;
  font-weight: 800;
}

.sim-card.barcode-ticket .sim-card-number {
  color: #475569;
}

.sim-card-barcode-lines {
  display: flex;
  height: 18px;
  gap: 1.5px;
  background: transparent;
  align-items: stretch;
  margin-top: 4px;
}

.sim-card-barcode-lines span {
  background-color: #0f172a;
  display: inline-block;
  width: 2px;
}

.sim-card-barcode-lines span:nth-child(2n) {
  width: 1px;
}
.sim-card-barcode-lines span:nth-child(3n) {
  width: 3px;
}
.sim-card-barcode-lines span:nth-child(5n) {
  width: 4px;
}
/* Kiosk Camera Overlay Styles */
.kiosk-camera-layer {
  position: absolute;
  inset: 0;
  background: rgba(15, 23, 42, 0.95);
  backdrop-filter: blur(15px);
  z-index: 10002;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 28px;
}

.kiosk-cam-box {
  width: 380px;
  display: flex;
  flex-direction: column;
  align-items: center;
  background: rgba(30, 41, 59, 0.4);
  border: 1px solid rgba(255, 255, 255, 0.1);
  padding: 24px;
  border-radius: 20px;
  box-shadow: 0 20px 50px rgba(0, 0, 0, 0.3);
}

.kiosk-cam-header {
  width: 100%;
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
  color: #94a3b8;
  font-size: 13px;
  font-weight: 600;
}

.kiosk-cam-badge {
  font-size: 10px;
  padding: 3px 8px;
  border-radius: 20px;
  font-weight: 700;
}

.kiosk-cam-error-box {
  background: rgba(30, 41, 59, 0.6);
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 16px;
  padding: 24px;
  text-align: center;
  width: 100%;
  box-sizing: border-box;
}

.kiosk-error-emoji {
  font-size: 40px;
  margin-bottom: 12px;
}

.kiosk-cam-error-box p {
  font-size: 13px;
  line-height: 1.6;
  color: #cbd5e1;
  margin: 0 0 20px 0;
}

.kiosk-error-actions {
  display: flex;
  gap: 12px;
  justify-content: center;
}

.kiosk-cam-preview-box {
  width: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
}

.kiosk-video-frame {
  position: relative;
  width: 320px;
  height: 240px;
  border-radius: 16px;
  overflow: hidden;
  border: 2px solid rgba(0, 223, 137, 0.2);
  background: #000;
}

.kiosk-video-frame video {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
}

.kiosk-cam-corners {
  position: absolute;
  width: 16px;
  height: 16px;
  border: 3px solid #00df89;
  pointer-events: none;
}
.kiosk-cam-corners.top-left { top: 12px; left: 12px; border-right: none; border-bottom: none; }
.kiosk-cam-corners.top-right { top: 12px; right: 12px; border-left: none; border-bottom: none; }
.kiosk-cam-corners.bottom-left { bottom: 12px; left: 12px; border-right: none; border-top: none; }
.kiosk-cam-corners.bottom-right { bottom: 12px; right: 12px; border-left: none; border-top: none; }

.kiosk-cam-switching-overlay {
  position: absolute;
  inset: 0;
  background: rgba(15, 23, 42, 0.85);
  display: flex;
  align-items: center;
  justify-content: center;
  color: #00df89;
  font-size: 14px;
  font-weight: 600;
}

.kiosk-cam-controls {
  display: flex;
  gap: 10px;
  margin-top: 20px;
  width: 100%;
  justify-content: center;
  align-items: center;
  flex-wrap: wrap;
}

.btn-kiosk-shoot {
  background: #00df89;
  border: none;
  color: #0a2d5a;
  border-radius: 20px;
  padding: 10px 20px;
  font-size: 13px;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s;
  box-shadow: 0 4px 12px rgba(0, 223, 137, 0.3);
}
.btn-kiosk-shoot:hover:not(:disabled) {
  background: #05ff9f;
  transform: translateY(-1px);
  box-shadow: 0 6px 16px rgba(5, 255, 159, 0.4);
}
.btn-kiosk-shoot:disabled {
  background: #cbd5e1;
  color: #94a3b8;
  cursor: not-allowed;
  box-shadow: none;
}

.btn-kiosk-skip {
  background: rgba(255, 255, 255, 0.1);
  border: 1px solid rgba(255, 255, 255, 0.15);
  color: #f1f5f9;
  border-radius: 20px;
  padding: 9px 18px;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
}
.btn-kiosk-skip:hover {
  background: rgba(255, 255, 255, 0.2);
}

.btn-kiosk-flip {
  background: rgba(255, 255, 255, 0.08);
  border: 1px solid rgba(255, 255, 255, 0.12);
  color: #cbd5e1;
  border-radius: 50%;
  width: 38px;
  height: 38px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.2s;
}
.btn-kiosk-flip:hover {
  background: rgba(255, 255, 255, 0.18);
}

.kiosk-cam-hint-text {
  margin-top: 12px;
  font-size: 11px;
  color: #64748b;
  text-align: center;
}
</style>
