namespace ClinicManagement.Services;

public sealed record AiSymptomRuleSeed(string DepartmentName, int Score, string[] Terms);

public static class AiSymptomRuleSeedCatalog
{
    public static readonly AiSymptomRuleSeed[] Rules =
    [
        new("Tiêu hóa", 28,
        [
            "chướng bụng", "đầy hơi", "đầy bụng", "bụng chướng", "khó tiêu", "ăn không tiêu",
            "đau thượng vị", "đau dạ dày", "ợ chua", "ợ nóng", "trào ngược", "nóng rát thượng vị",
            "buồn nôn", "nôn", "ói", "nôn ói", "tiêu chảy", "đi ngoài nhiều", "phân lỏng",
            "đau quanh rốn", "đau bụng sau ăn", "đau bụng âm ỉ", "đau bụng từng cơn", "sôi bụng"
        ]),
        new("Tiêu hóa", 20,
        [
            "táo bón", "đi ngoài khó", "phân đen", "đi ngoài ra máu", "đau hạ sườn phải",
            "vàng da", "vàng mắt", "chán ăn", "sụt cân kèm đau bụng", "nuốt nghẹn", "đắng miệng",
            "khô miệng kèm buồn nôn", "rối loạn tiêu hóa", "viêm đại tràng", "đại tràng co thắt",
            "đau đại tràng", "đau bụng dưới", "đau bụng bên trái", "đau bụng bên phải",
            "ngộ độc thực phẩm", "đau bụng tiêu chảy", "nôn ra máu"
        ]),
        new("Tiêu hóa", 14,
        [
            "dạ dày", "bao tử", "gan", "mật", "tụy", "ruột", "đại tràng", "hậu môn",
            "trĩ", "sa búi trĩ", "rát hậu môn", "ngứa hậu môn", "ăn uống kém", "cồn cào bụng",
            "đau bụng khi đói", "đau bụng về đêm", "ăn cay đau bụng", "đau bụng kèm sốt",
            "khám tiêu hóa", "nội soi dạ dày", "nội soi đại tràng"
        ]),

        new("Hô hấp", 28,
        [
            "khó thở", "hụt hơi", "thở khò khè", "thở rít", "khò khè", "hen", "hen suyễn",
            "cơn hen", "viêm phổi", "đau ngực khi thở", "tức ngực kèm khó thở", "ho ra máu",
            "ho kéo dài", "ho nhiều về đêm", "ho có đờm", "ho khan kéo dài", "khạc đờm",
            "đờm xanh", "đờm vàng", "sốt kèm ho", "thở nhanh", "khó thở khi nằm"
        ]),
        new("Hô hấp", 20,
        [
            "ho", "ho khan", "ho gió", "viêm phế quản", "viêm tiểu phế quản", "cảm cúm kèm ho",
            "đau ngực kèm ho", "đau lưng khi ho", "mệt khi gắng sức", "ran phổi", "viêm đường hô hấp",
            "nhiễm khuẩn hô hấp", "đau tức phổi", "dị ứng đường thở", "khó thở sau vận động",
            "hít thở đau", "hít sâu đau ngực", "ngưng thở khi ngủ"
        ]),
        new("Hô hấp", 14,
        [
            "phổi", "phế quản", "khí quản", "đờm", "ngạt thở", "thiếu oxy", "thở mệt",
            "ho sau cảm", "ho do lạnh", "ho do dị ứng", "ho sau covid", "khó thở sau covid",
            "đau ngực khi ho", "khàn tiếng kèm ho", "sổ mũi kèm ho", "đau họng kèm ho"
        ]),

        new("Mắt", 28,
        [
            "đau mắt", "đỏ mắt", "mờ mắt", "nhìn mờ", "viêm kết mạc", "chảy nước mắt",
            "ngứa mắt", "mắt đỏ", "nhức mắt", "sưng mắt", "mắt sưng", "cộm mắt",
            "cộm như có cát", "khô mắt", "chói mắt", "cay mắt", "giảm thị lực", "mất thị lực",
            "nhìn đôi", "ruồi bay", "chớp sáng trước mắt"
        ]),
        new("Mắt", 20,
        [
            "cận thị", "viễn thị", "loạn thị", "mỏi mắt", "mắt nháy", "lé mắt", "sụp mí",
            "đau quanh mắt", "đau hốc mắt", "lẹo mắt", "chắp mắt", "ghèn mắt", "mắt nhiều ghèn",
            "dị vật trong mắt", "bỏng mắt", "chấn thương mắt", "tăng nhãn áp", "đục thủy tinh thể",
            "nhìn quầng sáng", "mắt lóa"
        ]),
        new("Mắt", 14,
        [
            "thị lực", "võng mạc", "giác mạc", "mí mắt", "kính mắt", "đo mắt", "khám mắt",
            "mắt mỏi khi dùng máy tính", "mắt khô khi làm việc", "mắt đỏ sau bơi", "mắt nhạy sáng",
            "đau mắt khi đọc sách", "dị ứng mắt", "viêm bờ mi", "chảy dịch mắt"
        ]),

        new("Tai mũi họng", 28,
        [
            "đau họng", "viêm họng", "khàn tiếng", "mất tiếng", "nghẹt mũi", "viêm xoang",
            "đau tai", "ù tai", "viêm amidan", "nuốt đau", "nuốt vướng", "sổ mũi",
            "chảy mũi", "nước mũi xanh", "nước mũi vàng", "đau xoang", "đau nhức xoang",
            "nghe kém", "chảy máu cam", "viêm tai"
        ]),
        new("Tai mũi họng", 20,
        [
            "hắt hơi", "ngứa họng", "dịch mũi", "mũi đặc", "đau vùng trán", "cổ mũi",
            "dịch họng", "đau tai khi nuốt", "chảy dịch tai", "tai có mủ", "ngứa tai",
            "viêm mũi dị ứng", "polyp mũi", "ngủ ngáy", "ngưng thở khi ngủ", "đau thanh quản",
            "viêm thanh quản", "đau sau tai", "đau hàm kèm ù tai"
        ]),
        new("Tai mũi họng", 14,
        [
            "tai", "mũi", "họng", "amidan", "xoang", "thanh quản", "khí quản trên", "ráy tai",
            "lấy ráy tai", "mùi hôi miệng", "hôi miệng do họng", "vướng đờm cổ", "dị ứng mũi",
            "nước mũi trong", "đau đầu vùng trán", "đau đầu do xoang", "ho kèm đau họng"
        ]),

        new("Da liễu", 28,
        [
            "ngứa da", "nổi mẩn", "nổi mề đay", "phát ban", "nổi mụn", "mụn trứng cá",
            "viêm da", "dị ứng da", "nấm da", "da đỏ", "da bong vảy", "vảy nến",
            "eczema", "chàm", "mụn nước", "loét da", "ngứa toàn thân", "mụn nhọt",
            "áp xe da", "nhiễm trùng da", "mụn mủ", "mụn viêm"
        ]),
        new("Da liễu", 20,
        [
            "rối loạn sắc tố", "tàn nhang", "nám da", "ngứa đầu", "rụng tóc", "nấm móng",
            "nổi sẩn", "da khô", "da nứt nẻ", "da rát", "da chảy dịch", "mẩn đỏ",
            "ban đỏ", "mụn lưng", "mụn mặt", "mụn đầu đen", "mụn đầu trắng", "sẹo mụn",
            "dị ứng mỹ phẩm", "viêm nang lông", "rôm sảy"
        ]),
        new("Da liễu", 14,
        [
            "da", "mụn", "ngứa", "mẩn", "mề đay", "bong da", "sẹo", "nốt ruồi",
            "mụn cóc", "mụn thịt", "lang ben", "hắc lào", "ghẻ", "zona", "giời leo",
            "da nhạy cảm", "đốm trắng da", "đốm nâu da", "móng tay đổi màu"
        ]),

        new("Cơ xương khớp", 28,
        [
            "đau khớp", "sưng khớp", "đau gối", "đau vai", "đau lưng", "đau cột sống",
            "đau cổ vai gáy", "tê tay", "tê chân", "đau cổ", "căng cơ", "bong gân",
            "chấn thương", "đau xương", "đau cổ tay", "đau cổ chân", "hạn chế vận động",
            "thoái hóa khớp", "viêm khớp", "đau thần kinh tọa", "cứng cổ", "cứng khớp"
        ]),
        new("Cơ xương khớp", 20,
        [
            "loãng xương", "mỏi cơ", "gai cột sống", "đau thắt lưng", "đau lưng lan xuống chân",
            "đau vai gáy", "đau khuỷu tay", "đau bàn tay", "đau bàn chân", "đau háng",
            "đau cơ", "co cứng cơ", "trật khớp", "gãy xương", "đau sau té ngã",
            "đau sau vận động", "chuột rút", "tê bì tay chân", "yếu tay", "yếu chân"
        ]),
        new("Cơ xương khớp", 14,
        [
            "xương khớp", "khớp", "cột sống", "gân", "cơ", "dây chằng", "đau nhức toàn thân",
            "mỏi vai", "mỏi lưng", "đau khi cúi", "đau khi xoay cổ", "đau khi đi lại",
            "khớp kêu lục cục", "sưng đầu gối", "nóng khớp", "đau cổ chân khi đi"
        ]),

        new("Tim mạch", 28,
        [
            "đau ngực", "tức ngực", "đánh trống ngực", "hồi hộp", "tim đập nhanh", "tim đập chậm",
            "loạn nhịp", "khó thở khi gắng sức", "phù chân", "phù mắt cá", "tăng huyết áp",
            "cao huyết áp", "hạ huyết áp", "choáng khi đứng", "ngất", "đau ngực lan tay trái",
            "đau ngực lan hàm", "mệt khi leo cầu thang", "khó thở về đêm"
        ]),
        new("Tim mạch", 20,
        [
            "đau thắt ngực", "nặng ngực", "mạch nhanh", "mạch chậm", "huyết áp cao",
            "huyết áp thấp", "rối loạn nhịp tim", "suy tim", "thiếu máu cơ tim", "đau tim",
            "chóng mặt kèm hồi hộp", "vã mồ hôi kèm đau ngực", "mệt kèm phù chân",
            "đau ngực sau gắng sức", "khó thở khi nằm", "tim đập bỏ nhịp"
        ]),
        new("Tim mạch", 14,
        [
            "tim", "mạch", "huyết áp", "điện tim", "khám tim", "đo huyết áp", "mỡ máu",
            "cholesterol", "đau đầu do huyết áp", "hoa mắt do huyết áp", "choáng váng",
            "mệt tim", "nhịp tim", "đau ngực trái"
        ]),

        new("Sản phụ khoa", 28,
        [
            "đau bụng kinh", "rối loạn kinh nguyệt", "trễ kinh", "rong kinh", "ra khí hư",
            "khí hư bất thường", "ngứa vùng kín", "đau vùng chậu", "đau hạ vị", "ra máu âm đạo",
            "ra máu bất thường", "đau khi quan hệ", "viêm âm đạo", "viêm phụ khoa",
            "khám thai", "thai nghén", "buồn nôn khi mang thai", "đau bụng khi mang thai",
            "ra máu khi mang thai"
        ]),
        new("Sản phụ khoa", 20,
        [
            "mất kinh", "kinh nguyệt không đều", "đau vú", "căng tức vú", "dịch âm đạo",
            "mùi hôi vùng kín", "nấm âm đạo", "viêm cổ tử cung", "u xơ tử cung", "u nang buồng trứng",
            "khám phụ khoa", "tầm soát cổ tử cung", "đặt vòng", "tư vấn tránh thai",
            "đau bụng dưới ở nữ", "đau lưng khi hành kinh", "ra huyết trắng"
        ]),
        new("Sản phụ khoa", 14,
        [
            "phụ khoa", "sản khoa", "thai", "mang thai", "kinh nguyệt", "vùng kín", "âm đạo",
            "tử cung", "buồng trứng", "cổ tử cung", "khí hư", "huyết trắng", "siêu âm thai",
            "thử thai", "chậm kinh"
        ]),

        new("Nội tổng quát", 24,
        [
            "đau đầu", "nhức đầu", "chóng mặt", "hoa mắt", "xây xẩm", "sốt cao", "sốt nhẹ",
            "sốt kéo dài", "mệt mỏi", "suy nhược", "mất ngủ", "ớn lạnh", "đau mỏi người",
            "khám tổng quát", "khám sức khỏe", "kiểm tra sức khỏe", "tầm soát sức khỏe",
            "không rõ nguyên nhân", "đau nhiều nơi", "mệt kéo dài"
        ]),
        new("Nội tổng quát", 18,
        [
            "tiểu đường", "đường huyết", "tăng đường huyết", "hạ đường huyết", "giảm cân",
            "sụt cân", "tăng cân", "phù mặt", "phù chân", "khát nước nhiều", "đi tiểu nhiều",
            "ăn nhiều vẫn sụt cân", "thiếu máu", "xanh xao", "mệt không rõ lý do",
            "đau ngực không rõ", "đau bụng không rõ", "đau đầu kèm sốt"
        ]),
        new("Nội tổng quát", 12,
        [
            "nội khoa", "sức khỏe tổng quát", "khám định kỳ", "tư vấn sức khỏe", "sốt",
            "đau nhức", "mệt", "yếu người", "ăn kém", "chán ăn", "run tay", "ra mồ hôi",
            "nổi hạch", "hạch cổ", "hạch nách", "cảm giác khó chịu toàn thân"
        ])
    ];
}
