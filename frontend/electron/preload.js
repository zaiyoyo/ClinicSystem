const { contextBridge, ipcRenderer } = require('electron')

contextBridge.exposeInMainWorld('electronAPI', {
  // 未来可以暴露打印机、读卡器等本地设备接口
  platform: process.platform
})
