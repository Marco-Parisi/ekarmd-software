# **Muon Detector Reader**  
**Muon Detector Reader** is a **C#** software that analyzes data produced by [EKAR Muon Detector](https://github.com/Marco-Parisi/ekarmd-hardware), correcting them based on atmospheric pressure and temperature. The software generates various interactive graphs to study the trend of muon flux over time and provides advanced data export and analysis features.

## **Features**
The software has been recently updated to include several new capabilities:
- **Interactive Charts**: Visualize Pressure and Temperature Corrected Counts, Barometric Coefficient (Beta), and kT parameter. 
- **Chart Export**: Export chart to `.png` file.
- **Data Export**: Export processed data (including Beta and kT coefficients and statistical errors) to `.txt` (Tab-Separated Values) format.
- **Outlier Removal**: Automatically filter out spikes using a configurable $\sigma$ threshold.
- **Multi-language UI**: Support for English and Italian.
- **Themes**: Customizable Light and Dark mode support.
- **Command-Line Interface (CLI)**: Headless mode for automatic data processing and chart exporting.

## **Data Download**
EKAR Data can be downloaded here: [ekarmuondetector.org](https://ekarmuondetector.org/)

## **Data Format**  
The software reads **text files (.txt)** with the following format:  

```
YYYY-MM-DD HH:MM:SS * Temperature (°C) * Pressure (hPa) * Counts
```
- Example :
```
2022/08/05 13:44:37 * 35.80 * 979.26 * 8816
2022/08/05 14:44:37 * 36.40 * 978.79 * 8719
2022/08/05 15:44:37 * 36.80 * 978.43 * 8833
2022/08/05 16:44:37 * 37.00 * 978.22 * 8892
```
**The "*" separator can even be ",".**

## **CLI / Automation Usage**
The software can be run without opening the graphical interface to automate data processing. You can run the executable from the command line passing the necessary arguments:
```cmd
MuonDetectorReader.exe "<folder_path>" "<detector_name>" [days]
```
- `"<folder_path>"`: The absolute path to the directory containing the data files.
- `"<detector_name>"`: The name of the detector (used for the output files).
- `[days]`: (Optional) The number of days to analyze. Default is 14 days.

## **Software Output**  
The processed data is displayed as interactive graphs, which can be saved as `.png` files. Additionally, the complete dataset, including the estimated correction parameters, can be exported to a `.txt` file for further analysis.

## **Screenshot**  
<div>
  <p align="center">
    <font size=2><i>Main page</i></font>
  </p>
  
  <p align="center">
    <img src="https://github.com/user-attachments/assets/bb37cda4-2142-499e-ace9-ad58aaf719cc" width="700"/>
  </p>
</div>

<div>
  <p align="center">
    <font size=2><i>Pressure and Temperature Corrected Counts</i></font>
  </p>
  <p align="center">
    <img src="https://github.com/user-attachments/assets/31c35836-c352-47df-a2ea-9fd972bb892a" width="700"/>
  </p>
</div>

<div>  
  <p align="center">
    <font size=2><i>Dual Graph Mode</i></font>
  </p>
  <p align="center">
    <img src="https://github.com/user-attachments/assets/c2ef3d15-6284-4c33-bdcb-f102e0ac8d62" width="700"/>
  </p>
</div>
