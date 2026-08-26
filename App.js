import React, { useState } from 'react';
import {
  StyleSheet,
  Text,
  View,
  TextInput,
  TouchableOpacity,
  Image,
  ScrollView,
  Alert,
  ActivityIndicator,
  Platform,
} from 'react-native';
import * as ImagePicker from 'expo-image-picker';
import * as FileSystem from 'expo-file-system/legacy';
import * as Sharing from 'expo-sharing';

const API_BASE_URL = 'http://192.168.1.188:5046';

export default function App() {
  const [tab, setTab] = useState('encode'); // Kullanıcı hangi sekmede: "Mesaj Gizle" mi "Mesaj Çöz" mü.
  const [selectedImage, setSelectedImage] = useState(null);// fotoğrafın konumu
  const [message, setMessage] = useState(''); // kullanıcının yazdığı gizli mesaj,
  const [password, setPassword] = useState(''); // girdiği parola.
  const [loading, setLoading] = useState(false);
  const [decodedMessage, setDecodedMessage] = useState('');
  const [lastGeneratedStegoUri, setLastGeneratedStegoUri] = useState(null); // üretilen yeni fotoğrafın hem telefonun geçici hafızasındaki konumunu (Uri)
  const [lastGeneratedBase64, setLastGeneratedBase64] = useState(null); //hem de ham verisini (Base64 — metne çevrilmiş hâli, kaydetme/paylaşma işlemleri için lazım)

  const pickImage = async () => {
    const { status } = await ImagePicker.requestMediaLibraryPermissionsAsync();
    // galeriye erişim izni
    if (status !== 'granted') {
      Alert.alert('İzin Gerekli', 'Galeriye erişim izni vermelisiniz.');
      return;
    }

    const result = await ImagePicker.launchImageLibraryAsync({
      //quality:1 en yüksek kalite sıkıştırma yapma. pisekllerin bozulmaması için şart 
      mediaTypes: ['images'],
      allowsEditing: false,
      quality: 1,
    });

    if (!result.canceled) {
      setSelectedImage(result.assets[0].uri);
      setDecodedMessage('');
    }
  };

  // Mod 1: Mesaj Gizle (Encode)
  const handleEncode = async () => {
    if (!selectedImage || !message || !password) {
      Alert.alert('Eksik Bilgi', 'Lütfen resim, gizli mesaj ve en az 6 haneli şifre giriniz.');
      return;
    }

    setLoading(true);
    try {
      const uriParts = selectedImage.split('/');
      const originalFileName = uriParts[uriParts.length - 1] || 'image.jpg';

      const formData = new FormData();
      formData.append('SecretMessage', message);
      formData.append('Password', password);
      formData.append('Image', {
        uri: selectedImage,
        name: originalFileName,
        type: 'image/jpeg',
      });
      // tek bir fotoğraf+ iki metin alanı , alan isimleri yani secratmessage,password,image backendde encdoerequestdtoyşa eşleşmeli

      const response = await fetch(`${API_BASE_URL}/api/Stego/encode`, {
        method: 'POST',
        body: formData,
        headers: {
          'Accept': '*/*',
        },
      });
      // accept: cevap ne formda olsa kabul ederim, çünkü cevap json değil direkt fotoğraf dosyası olacak. json bekleseydik accept: 'application/json' yazardık

      if (!response.ok) {
        const errText = await response.text();
        throw new Error(errText || 'Sunucu işlemi tamamlayamadı.');
      }

      const psnr = response.headers.get('x-psnr') || '90+';
      // Backend'in header'a koyduğu PSNR değerini okuyor — bunu "izin ver

      const blob = await response.blob();
      const reader = new FileReader();

      reader.onloadend = async () => {
        try {
          const base64data = reader.result.split(',')[1];
          const localTargetUri = `${FileSystem.cacheDirectory}stego_${Date.now()}.png`;

          await FileSystem.writeAsStringAsync(localTargetUri, base64data, {
            encoding: 'base64',
          });
 
          //gelen ham fotğraf verisini (blob), base64 metne çevirip (FileReader), telefonun geçici (cache) klasörüne bir dosya olarak yazıyor.
//reader.result.split(',')[1] — FileReader'ın çıktısı "data:image/png;base64,iVBORw0KG..." gibi bir format olduğu için, ,'den böl, sadece asıl veriyi (ikinci parça) al, baş kısmındaki "bu bir PNG'dir" etiketini at.
          setLastGeneratedStegoUri(localTargetUri);
          setLastGeneratedBase64(base64data);

          Alert.alert(
            'Başarılı! ✅',
            `Mesaj fotoğrafa gizlendi!\nPSNR: ${psnr} dB\n\nResmi telefonun İndirilenler klasörüne kaydetmek için 'Telefona Kaydet' butonuna basabilirsiniz.`
          );
        } catch (fileErr) {
          Alert.alert('Kayıt Hatası', fileErr.message);
        } finally {
          setLoading(false);
        }
      };

      reader.readAsDataURL(blob);
    } catch (error) {
      setLoading(false);
      Alert.alert('Bağlantı Hatası', error.message);
    }
  };

  // Mod 2: Mesaj Çöz (Decode)
  const handleDecode = async () => {
    if (!selectedImage || !password) {
      Alert.alert('Eksik Bilgi', 'Lütfen şifreli resmi seçin ve parolayı girin.');
      return;}
    setLoading(true);
    try {
      const uriParts = selectedImage.split('/');
      const fileName = uriParts[uriParts.length - 1] || 'stego.png';
      const formData = new FormData();
      formData.append('Password', password);
      formData.append('StegoImage', {
        uri: selectedImage,
        name: fileName,
        type: 'image/png',});
      const response = await fetch(`${API_BASE_URL}/api/Stego/decode`, {
        method: 'POST',
        body: formData,
        headers: {
          'Accept': 'application/json',},});
      const data = await response.json();
      if (response.ok && data.success) {
        setDecodedMessage(data.secretMessage);} else {
        Alert.alert('Hata ❌', data.error || 'Mesaj çözülemedi. Parola yanlış olabilir.');}
    } catch (error) {
      Alert.alert('Bağlantı Hatası', error.message);
    } finally {
      setLoading(false);
    }
  };

  // Android Storage Access Framework (SAF) ile Telefona Doğrudan Kaydetme
  const handleSaveToDevice = async () => {
    if (!lastGeneratedBase64) return;

    try {
      if (Platform.OS === 'android') {
        const permissions = await FileSystem.StorageAccessFramework.requestDirectoryPermissionsAsync();
        if (permissions.granted) {
          const directoryUri = permissions.directoryUri;
          const fileName = `stego_${Date.now()}`;
          const newFileUri = await FileSystem.StorageAccessFramework.createFileAsync(
            directoryUri,
            fileName,
            'image/png'
          );

          await FileSystem.writeAsStringAsync(newFileUri, lastGeneratedBase64, {
            encoding: 'base64',
          });

          Alert.alert('Kaydedildi ✅', 'Fotoğraf seçtiğiniz klasöre PNG olarak başarıyla kaydedildi!');
        }
      } else {
        // iOS için paylaşım menüsü
        await Sharing.shareAsync(lastGeneratedStegoUri);
      }
    } catch (err) {
      Alert.alert('Kaydetme Hatası', err.message);
    }
  };

  const handleShare = async () => {
    if (!lastGeneratedStegoUri) return;
    if (await Sharing.isAvailableAsync()) {
      await Sharing.shareAsync(lastGeneratedStegoUri, {
        mimeType: 'image/png',
        dialogTitle: 'Şifreli Görseli Paylaş',
      });
    }
  };

  const useGeneratedForDecode = () => {
    setSelectedImage(lastGeneratedStegoUri);
    setTab('decode');
    setDecodedMessage('');
  };
  // mesajı gizledikten sonra direkte otomatik çöz sekmesine geçiyor 

  return (
    <ScrollView contentContainerStyle={styles.container}>
      <Text style={styles.title}>Steganografi Studio</Text>

      {/* Tab Seçimi */}
      <View style={styles.tabContainer}>
        <TouchableOpacity
          style={[styles.tabButton, tab === 'encode' && styles.tabActive]}
          onPress={() => { setTab('encode'); setSelectedImage(null); }}
        >
          <Text style={[styles.tabText, tab === 'encode' && styles.tabTextActive]}>Mesaj Gizle</Text>
        </TouchableOpacity>
        <TouchableOpacity
          style={[styles.tabButton, tab === 'decode' && styles.tabActive]}
          onPress={() => { setTab('decode'); setSelectedImage(null); }}
        >
          <Text style={[styles.tabText, tab === 'decode' && styles.tabTextActive]}>Mesaj Çöz</Text>
        </TouchableOpacity>
      </View>

      {/* Resim Seçim Alanı */}
      <TouchableOpacity style={styles.imagePicker} onPress={pickImage}>
        {selectedImage ? (
          <Image source={{ uri: selectedImage }} style={styles.imagePreview} />
        ) : (
          <Text style={styles.imagePickerText}>📷 Fotoğraf Seçmek İçin Dokunun</Text>
        )}
      </TouchableOpacity>

      {/* Encode Alanı */}
      {tab === 'encode' && (
        <TextInput
          style={styles.input}
          placeholder="Gizlenecek Gizli Mesaj"
          placeholderTextColor="#888"
          value={message}
          onChangeText={setMessage}
          multiline
        />
      )}

      {/* Parola Alanı */}
      <TextInput
        style={styles.input}
        placeholder="AES Parolası (örn: 123456)"
        placeholderTextColor="#888"
        secureTextEntry
        value={password}
        onChangeText={setPassword}
      />

      {/* Ana Buton */}
      <TouchableOpacity
        style={styles.actionButton}
        onPress={tab === 'encode' ? handleEncode : handleDecode}
        disabled={loading}
      >
        {loading ? (
          <ActivityIndicator color="#fff" />
        ) : (
          <Text style={styles.actionButtonText}>
            {tab === 'encode' ? '🔒 Mesajı Gizle' : '🔓 Gizli Mesajı Çöz'}
          </Text>
        )}
      </TouchableOpacity>

      {/* Encode Sonrası Butonlar */}
      {tab === 'encode' && lastGeneratedStegoUri && (
        <View style={styles.extraButtonsContainer}>
          <TouchableOpacity style={styles.saveButton} onPress={handleSaveToDevice}>
            <Text style={styles.saveButtonText}>📥 Telefona / Klasöre Kaydet</Text>
          </TouchableOpacity>
          <TouchableOpacity style={styles.shareButton} onPress={handleShare}>
            <Text style={styles.shareButtonText}>📤 WhatsApp / Belge Olarak Paylaş</Text>
          </TouchableOpacity>
          <TouchableOpacity style={styles.transferButton} onPress={useGeneratedForDecode}>
            <Text style={styles.transferButtonText}>➡️ Çözme Sekmesine Aktar</Text>
          </TouchableOpacity>
        </View>
      )}

      {/* Çözülen Mesaj */}
      {tab === 'decode' && decodedMessage !== '' && (
        <View style={styles.resultCard}>
          <Text style={styles.resultLabel}>Fotoğraftan Çözülen Orijinal Mesaj:</Text>
          <Text style={styles.resultText}>{decodedMessage}</Text>
        </View>
      )}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { padding: 24, paddingTop: 60, backgroundColor: '#121212', minHeight: '100%' },
  title: { fontSize: 24, fontWeight: 'bold', color: '#fff', textAlign: 'center', marginBottom: 20 },
  tabContainer: { flexDirection: 'row', backgroundColor: '#1e1e1e', borderRadius: 10, marginBottom: 20 },
  tabButton: { flex: 1, paddingVertical: 12, alignItems: 'center', borderRadius: 10 },
  tabActive: { backgroundColor: '#3b82f6' },
  tabText: { color: '#888', fontWeight: 'bold' },
  tabTextActive: { color: '#fff' },
  imagePicker: {
    height: 180,
    backgroundColor: '#1e1e1e',
    borderRadius: 12,
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: 16,
    borderWidth: 1,
    borderColor: '#333',
    overflow: 'hidden',
  },
  imagePickerText: { color: '#aaa', fontSize: 15 },
  imagePreview: { width: '100%', height: '100%', resizeMode: 'cover' },
  input: {
    backgroundColor: '#1e1e1e',
    color: '#fff',
    borderRadius: 10,
    padding: 14,
    marginBottom: 14,
    fontSize: 15,
    borderWidth: 1,
    borderColor: '#333',
  },
  actionButton: {
    backgroundColor: '#10b981',
    paddingVertical: 16,
    borderRadius: 10,
    alignItems: 'center',
    marginTop: 8,
  },
  actionButtonText: { color: '#fff', fontSize: 16, fontWeight: 'bold' },
  extraButtonsContainer: { marginTop: 14, gap: 10 },
  saveButton: {
    backgroundColor: '#059669',
    paddingVertical: 14,
    borderRadius: 10,
    alignItems: 'center',
  },
  saveButtonText: { color: '#fff', fontSize: 14, fontWeight: 'bold' },
  shareButton: {
    backgroundColor: '#6366f1',
    paddingVertical: 14,
    borderRadius: 10,
    alignItems: 'center',
  },
  shareButtonText: { color: '#fff', fontSize: 14, fontWeight: 'bold' },
  transferButton: {
    backgroundColor: '#3b82f6',
    paddingVertical: 14,
    borderRadius: 10,
    alignItems: 'center',
  },
  transferButtonText: { color: '#fff', fontSize: 14, fontWeight: 'bold' },
  resultCard: {
    backgroundColor: '#1e293b',
    padding: 16,
    borderRadius: 10,
    marginTop: 20,
    borderLeftWidth: 4,
    borderLeftColor: '#38bdf8',
  },
  resultLabel: { color: '#94a3b8', fontSize: 13, marginBottom: 6 },
  resultText: { color: '#f8fafc', fontSize: 18, fontWeight: 'bold' },
});