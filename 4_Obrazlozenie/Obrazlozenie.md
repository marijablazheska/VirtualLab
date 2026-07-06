# Obrazlozenie za proektot: Virtuelna laboratorija

## 1. Kratok opis

Aplikacijata e izrabotena vo Unity kako 3D virtuelna laboratorija za eksperimenti so supstancii. Korisnikot moze da izbere supstancija od polica, da ja izmeri na vaga, da podgotvi primerok vo epruveta, da go dodade vo casa za mesanje, da izvrsi mesanje i da go zagree rastvorot vo kolba so plamenik.

Aplikacijata ima PC nacin za testiranje i mobilen 3D Box VR nacin. Vo VR nacin se koristi stereo prikaz, giroskopsko sledenje na glavata i interakcija so pogled.

## 2. Koristena tehnologija i konfiguracija

- Engine: Unity
- Verzija na Unity Editor: 6000.3.17f1
- Jazik: C#
- Glavna scena: Assets/Scenes/VirtualLab.unity
- Glavna skripta: Assets/Scripts/VirtualLabBootstrap.cs
- Target platforma: mobilen ured so 3D Box/Cardboard VR preglednik
- Dopolnitelno: PC test mode so gluvce/tastatura

Proektot ne koristi nadvoresni 3D modeli ili gotovi asset paketi. Laboratoriskite objekti se generiraat so Unity primitive objekti, materijali, tekst, particle effects i runtime generirani zvucni efekti.

## 3. Laboratoriski objekti i nivna uloga

- Vaga: se koristi za merenje na izbranata supstancija.
- Epruveta: po merenje se pojavuva mal primerok od supstancijata.
- Casa: glaven sad za mesanje; primerokot od epruvetata se dodava vo casa.
- Kolba: okrugla kolba za zagrevanje na podgotveniot rastvor.
- Plamenik: Bunsen plamenik koj ja zagreva kolbata.
- Supstancii: kiselina, baza, bakar sulfat i karbonat.

## 4. Logika na interakcii

Osnovniot tek na rabota e:

1. Korisnikot izbira supstancija od policata.
2. So Meri, supstancijata se meri na vaga i primerok se pojavuva vo epruveta.
3. So Dodaj, primerokot od epruvetata se prenesuva vo casa.
4. Postapkata se povtoruva za vtora supstancija.
5. So Mesaj, supstanciite reagiraat vo casa.
6. So Zagrej, rastvorot se prikazuva vo kolba i se zagreva so plamenik.
7. So Reset, laboratorijata se vraka vo pocetna sostojba.

## 5. Eksperimenti

### Kiselina + Baza

Rezultat: neutralizacija. Rastvorot menuva boja vo svetlo zelena i se slusa zvucen efekt.

### Kiselina + Karbonat

Rezultat: osloboduvanje gas. Se pojavuvaat meurcinja/cad i se slusa reakcija.

### Bakar sulfat + Baza

Rezultat: formiranje sin talog. Rastvorot dobiva posilna sina boja i se slusa zvucen efekt.

## 6. Vizuelni i zvucni efekti

Aplikacijata prikazuva:

- promena na boja na tecnosta
- tecnost vo epruveta po merenje
- tecnost vo casa po dodavanje
- tecnost vo kolba pri zagrevanje
- plamen na plamenikot
- cad i meurcinja kaj reakcija so gas
- rezultat/observacija na panelot
- zvuk pri izbor, merenje, dodavanje, mesanje i reakcija

## 7. VR nacin

Mobilniot 3D Box VR nacin vklucuva:

- stereo prikaz so leva i desna kamera
- giroskopsko sledenje na glavata na mobilen ured
- gaze reticle za izbor so pogled
- aktiviranje objekt ili kopce so zadrzan pogled okolu 1 sekunda

Na PC, VR nacinot se testira so kopceto V, a dvizenjeto se simulira so gluvce.

## 8. Kratko upatstvo za rabota

Primer za Acid + Carbonate reakcija:

1. Izberi Kiselina.
2. Pritisni Meri.
3. Pritisni Dodaj.
4. Izberi Karbonat.
5. Pritisni Meri.
6. Pritisni Dodaj.
7. Pritisni Mesaj.
8. Nabljuduvaj cad/meurcinja, promena i zvucen efekt.

## 9. Referenci i koristeni resursi

- Unity Manual / Unity Documentation za osnoven 3D workflow, GameObjects, Materials, Cameras, AudioSource i ParticleSystem.
- Unity C# API za runtime kreiranje objekti i interakcija so OnMouseDown/Raycast.
- Osnovni hemiski koncepti za neutralizacija, reakcija na kiselina so karbonat i talozenje so bakar sulfat i baza.

Ne se koristeni gotovi 3D modeli, teksturi ili nadvoresni asset paketi.