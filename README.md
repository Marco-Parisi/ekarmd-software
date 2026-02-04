# **Muon Detector Reader**  
**Muon Detector Reader** is a **C#** software that analyzes data produced by [EKAR Muon Detector](https://github.com/Marco-Parisi/ekar-hardware), correcting them based on atmospheric pressure and temperature. The software generates various interactive graphs to study the trend of muon flux over time.  

## **Data Download**
EKAR Data can be downloaded here: [ekarmuondetector.org](https://ekarmuondetector.org/)

## **Data Format**  
The software reads **text files (.txt)** with the following format:  

```
YYYY-MM-DD HH:MM:SS * Temperature (°C) * Pressure (hPa) *  Counts
```
- Example :
```
2022/08/05 13:44:37 * 35.80 * 979.26 * 8816
2022/08/05 14:44:37 * 36.40 * 978.79 * 8719
2022/08/05 15:44:37 * 36.80 * 978.43 * 8833
2022/08/05 16:44:37 * 37.00 * 978.22 * 8892
```
**The "*" separator can even be ",".**

## **Software Output**  
The processed data is displayed as interactive graphs, which can be saved as **PNG** files.  

## **Screenshot**  
<div>
  <p align="center">
    <font size=2><i>Main page</i></font>
  </p>
  <p align="center">
    <img src="https://github.com/user-attachments/assets/2259add3-cd8c-4018-a6bd-9f1fa29cec78" width="700"/>
  </p>
</div>

<div>
  <p align="center">
    <font size=2><i>Pressure and Temperature Corrected Counts</i></font>
  </p>
  <p align="center">
    <img src="https://github.com/user-attachments/assets/0d369e6d-4ae0-4a1d-a268-058c0f87cae6" width="700"/>
  </p>
</div>

<div>  
  <p align="center">
    <font size=2><i>Barometric Coefficient</i></font>
  </p>
  <p align="center">
    <img src="https://github.com/user-attachments/assets/1021ec14-cb40-48ff-8102-0febb60f9186" width="700"/>
  </p>
</div>

<div>
  <p align="center">
    <font size=2><i>Sigma Counts</i></font>
  </p>
  <p align="center">
    <img src="https://github.com/user-attachments/assets/5a43067d-4f5c-4093-872f-c2b47bf4159e" width="700"/>
  </p>
</div>
