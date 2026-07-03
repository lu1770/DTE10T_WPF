import pandas as pd
import numpy as np
import os
import re
from datetime import datetime

# ========== 1. 配置参数 ==========
FILE_PATH = r'C:\Users\zheng\Documents\温度测试实验数据 分析.xlsx'

# 要处理的Sheet列表（None表示处理所有Sheet）
SHEETS_TO_PROCESS = None  # 例如: ['PID模式205->200降温', 'PID模式200->205升温']

# 【自动检测】温度列模式 - 自动匹配 CH1(℃) ~ CH8(℃)
COLUMN_PATTERN = r'^CH\d+\(℃\)$'  # 正则表达式匹配 CH1(℃), CH2(℃), ...

# 【自动检测】功率列模式 - 自动匹配 CH1输出1(%) ~ CH8输出1(%)
POWER_COLUMN_PATTERN = r'^CH\d+输出1\(\%\)$'  # 正则表达式匹配 CH1输出1(%), CH2输出1(%), ...

# 默认温度参数（当Sheet名称无法解析时使用）
DEFAULT_TEMP_ORIGIN = 205.0
DEFAULT_TEMP_TARGET = 200.0

# 温度容差
TEMP_TOLERANCE = 0.005          # 首次到达目标用宽容差
TEMP_TOLERANCE_STRICT = 0.1   # 保温开始用严格容差
OVERSHOOT_MIN_DETECT = 0.01
N_STABLE = 5

# 时间格式
TIME_FORMAT = '%Y/%m/%d %H:%M:%S'

# 输出报告文件名
REPORT_NAME = '温度阶段分析报告.xlsx'
# =================================


def parse_temperature_from_sheet_name(sheet_name):
    """
    从Sheet名称中解析原始温度和目标温度
    例如: 'PID模式205->200降温' -> (205.0, 200.0)
    """
    pattern = r'(\d+\.?\d*)\s*->\s*(\d+\.?\d*)'
    match = re.search(pattern, sheet_name)
    
    if match:
        temp_origin = float(match.group(1))
        temp_target = float(match.group(2))
        return temp_origin, temp_target
    else:
        return None, None


def detect_temperature_direction(sheet_name):
    """检测温度变化方向（升温/降温）"""
    if '升温' in sheet_name:
        return '升温'
    elif '降温' in sheet_name:
        return '降温'
    else:
        return '未知'


def get_duration_label(direction):
    """根据方向返回耗时标签"""
    return '升温耗时' if direction == '升温' else '降温耗时'


def get_temperature_columns(df, pattern):
    """从DataFrame中获取匹配模式的所有温度列"""
    cols = []
    for col in df.columns:
        if re.match(pattern, col):
            cols.append(col)
    return sorted(cols)  # 按字母排序


def get_power_columns(df, pattern):
    """从DataFrame中获取匹配模式的所有功率列"""
    cols = []
    for col in df.columns:
        if re.match(pattern, col):
            cols.append(col)
    return sorted(cols)  # 按字母排序


def get_sv_columns(df):
    """从DataFrame中获取所有SV设定值列（CH1SV(℃)-CH8SV(℃)）"""
    cols = []
    for col in df.columns:
        if re.match(r'^CH\d+SV\(℃\)$', col):
            cols.append(col)
    return sorted(cols)


def infer_target_temperature_from_sv(df):
    """
    从SV列（CH1SV(℃)-CH8SV(℃)）推断目标温度
    对于目标温度不一致的，取目标温度列数量最多的那个数作为目标温度
    """
    sv_cols = get_sv_columns(df)
    if not sv_cols:
        return None, None
    
    temp_counts = {}
    
    for col in sv_cols:
        # 获取该列非空值的众数（出现次数最多的值）
        val_counts = df[col].dropna().value_counts()
        if len(val_counts) > 0:
            # 取出现次数最多的值作为该列的目标温度
            most_common = val_counts.index[0]
            count = val_counts.iloc[0]
            if most_common in temp_counts:
                temp_counts[most_common] += 1
            else:
                temp_counts[most_common] = 1
    
    if not temp_counts:
        return None, None
    
    # 找出在最多列中出现的温度值作为目标温度
    target_temp = max(temp_counts, key=temp_counts.get)
    
    # 找出原始温度（通常是出现次数第二多的值，或者是初始值）
    # 这里我们取所有SV列第一个非空值作为原始温度
    origin_temp = None
    for col in sv_cols:
        first_val = df[col].dropna().iloc[0] if len(df[col].dropna()) > 0 else None
        if first_val is not None:
            origin_temp = first_val
            break
    
    return origin_temp, target_temp


def analyze_temperature_data(df, column_name, time_column, params):
    """
    分析单个温度列的数据
    """
    TEMP_ORIGIN = params['TEMP_ORIGIN']
    TEMP_TARGET = params['TEMP_TARGET']
    TEMP_TOLERANCE = params['TEMP_TOLERANCE']
    TEMP_TOLERANCE_STRICT = params['TEMP_TOLERANCE_STRICT']
    OVERSHOOT_MIN_DETECT = params['OVERSHOOT_MIN_DETECT']
    N_STABLE = params['N_STABLE']
    DIRECTION = params.get('DIRECTION', '降温')
    
    result = {
        'column': column_name,
        'status': '成功',
        'start_time': None,
        'start_temp': None,
        'first_target_time': None,
        'first_target_temp': None,
        'cooling_duration': None,
        'overshoot_temp': None,
        'overshoot_time': None,
        'insulation_start_time': None,
        'insulation_start_temp': None,
        'insulation_duration': None,
        'stable_time': None,
        'stable_temp': None,
        'stable_duration': None,
        'has_overshoot': False,
        'error_msg': None
    }
    
    try:
        # 检查列是否存在
        if column_name not in df.columns:
            result['status'] = '失败'
            result['error_msg'] = f'列 {column_name} 不存在'
            return result
        
        if time_column not in df.columns:
            result['status'] = '失败'
            result['error_msg'] = f'时间列 {time_column} 不存在'
            return result
        
        # 删除空值
        df_valid = df[df[column_name].notna()].copy()
        if len(df_valid) == 0:
            result['status'] = '失败'
            result['error_msg'] = '无有效数据'
            return result
        
        # 解析时间
        df_valid['解析时间'] = pd.to_datetime(df_valid[time_column], format=TIME_FORMAT, errors='coerce')
        df_valid = df_valid[df_valid['解析时间'].notna()].copy()
        
        if len(df_valid) == 0:
            result['status'] = '失败'
            result['error_msg'] = '时间解析失败'
            return result
        
        # 排序并去重
        df_valid = df_valid.sort_values('解析时间').reset_index(drop=True)
        df_valid = df_valid.drop_duplicates(subset=['解析时间'], keep='first').reset_index(drop=True)
        
        # ----- 阶段1：开始 -----
        df_valid['距原始温度差值'] = abs(df_valid[column_name] - TEMP_ORIGIN)
        start_idx = df_valid['距原始温度差值'].idxmin()
        start_time = df_valid.loc[start_idx, '解析时间']
        start_temp = df_valid.loc[start_idx, column_name]
        
        result['start_time'] = start_time
        result['start_temp'] = start_temp
        
        # ----- 阶段2：首次到达目标 -----
        after_start = df_valid.loc[start_idx:].copy()
        target_reached_mask = abs(after_start[column_name] - TEMP_TARGET) <= TEMP_TOLERANCE
        
        if not target_reached_mask.any():
            result['status'] = '失败'
            result['error_msg'] = f'未到达目标温度 {TEMP_TARGET}℃'
            return result
        
        first_target_idx = after_start[target_reached_mask].index[0]
        first_target_time = df_valid.loc[first_target_idx, '解析时间']
        first_target_temp = df_valid.loc[first_target_idx, column_name]
        
        result['first_target_time'] = first_target_time
        result['first_target_temp'] = first_target_temp
        result['cooling_duration'] = (first_target_time - start_time).total_seconds()
        
        # ----- 阶段3：超调检测 -----
        after_first_target = df_valid.loc[first_target_idx:].copy()
        first_target_pos = after_first_target.index.get_loc(first_target_idx)
        
        lookahead = min(10, len(after_first_target) - first_target_pos - 1)
        has_overshoot = False
        
        if lookahead > 0:
            future_temps = after_first_target.iloc[first_target_pos+1:first_target_pos+1+lookahead][column_name].values
            
            if DIRECTION == '降温':
                if len(future_temps) > 0 and np.all(future_temps <= first_target_temp + 0.01):
                    has_overshoot = False
                    insulation_start_time = first_target_time
                    insulation_start_temp = first_target_temp
                else:
                    search_window = after_first_target.iloc[first_target_pos:first_target_pos+200]
                    max_temp_idx = search_window[column_name].idxmax()
                    max_temp = df_valid.loc[max_temp_idx, column_name]
                    max_time = df_valid.loc[max_temp_idx, '解析时间']
                    overshoot_amount = max_temp - TEMP_TARGET
                    
                    if overshoot_amount >= OVERSHOOT_MIN_DETECT:
                        has_overshoot = True
                        result['overshoot_temp'] = max_temp
                        result['overshoot_time'] = max_time
                        result['has_overshoot'] = True
                        
                        after_peak = df_valid.loc[max_temp_idx:].copy()
                        regress_mask = abs(after_peak[column_name] - TEMP_TARGET) <= TEMP_TOLERANCE_STRICT
                        
                        if regress_mask.any():
                            regress_idx = after_peak[regress_mask].index[0]
                            regress_time = df_valid.loc[regress_idx, '解析时间']
                            regress_temp = df_valid.loc[regress_idx, column_name]
                            
                            insulation_start_time = regress_time
                            insulation_start_temp = regress_temp
                            result['insulation_duration'] = (insulation_start_time - first_target_time).total_seconds()
                        else:
                            insulation_start_time = first_target_time
                            insulation_start_temp = first_target_temp
                    else:
                        insulation_start_time = first_target_time
                        insulation_start_temp = first_target_temp
            else:
                # 升温模式
                if len(future_temps) > 0 and np.all(future_temps >= first_target_temp - 0.01):
                    has_overshoot = False
                    insulation_start_time = first_target_time
                    insulation_start_temp = first_target_temp
                else:
                    search_window = after_first_target.iloc[first_target_pos:first_target_pos+200]
                    min_temp_idx = search_window[column_name].idxmin()
                    min_temp = df_valid.loc[min_temp_idx, column_name]
                    min_time = df_valid.loc[min_temp_idx, '解析时间']
                    overshoot_amount = TEMP_TARGET - min_temp
                    
                    if overshoot_amount >= OVERSHOOT_MIN_DETECT:
                        has_overshoot = True
                        result['overshoot_temp'] = min_temp
                        result['overshoot_time'] = min_time
                        result['has_overshoot'] = True
                        
                        after_peak = df_valid.loc[min_temp_idx:].copy()
                        regress_mask = abs(after_peak[column_name] - TEMP_TARGET) <= TEMP_TOLERANCE_STRICT
                        
                        if regress_mask.any():
                            regress_idx = after_peak[regress_mask].index[0]
                            regress_time = df_valid.loc[regress_idx, '解析时间']
                            regress_temp = df_valid.loc[regress_idx, column_name]
                            
                            insulation_start_time = regress_time
                            insulation_start_temp = regress_temp
                            result['insulation_duration'] = (insulation_start_time - first_target_time).total_seconds()
                        else:
                            insulation_start_time = first_target_time
                            insulation_start_temp = first_target_temp
                    else:
                        insulation_start_time = first_target_time
                        insulation_start_temp = first_target_temp
        else:
            insulation_start_time = first_target_time
            insulation_start_temp = first_target_temp
        
        result['insulation_start_time'] = insulation_start_time
        result['insulation_start_temp'] = insulation_start_temp
        
        if not has_overshoot:
            result['insulation_duration'] = 0.0
        
        # ----- 阶段4：保温稳定确认 -----
        after_insulation = df_valid.loc[df_valid['解析时间'] >= insulation_start_time].copy()
        stable_start_idx = None
        
        for i in range(len(after_insulation) - N_STABLE + 1):
            segment = after_insulation.iloc[i:i+N_STABLE]
            if (abs(segment[column_name] - TEMP_TARGET) <= TEMP_TOLERANCE_STRICT).all():
                stable_start_idx = after_insulation.index[i]
                break
        
        if stable_start_idx is not None:
            stable_time = df_valid.loc[stable_start_idx, '解析时间']
            stable_temp = df_valid.loc[stable_start_idx, column_name]
            result['stable_time'] = stable_time
            result['stable_temp'] = stable_temp
            result['stable_duration'] = (stable_time - insulation_start_time).total_seconds()
        
        return result
        
    except Exception as e:
        result['status'] = '失败'
        result['error_msg'] = str(e)
        return result


def analyze_power_data(df, column_name, time_column, first_target_time=None):
    """
    分析单个功率列的数据
    """
    result = {
        'column': column_name,
        'status': '成功',
        'max_power': None,
        'max_power_time': None,
        'min_power': None,
        'min_power_time': None,
        'avg_power': None,
        'avg_power_before_target': None,
        'avg_power_after_target': None,
        'peak_start_time': None,
        'peak_end_time': None,
        'peak_duration': None,
        'error_msg': None
    }
    
    try:
        if column_name not in df.columns:
            result['status'] = '失败'
            result['error_msg'] = f'列 {column_name} 不存在'
            return result
        
        if time_column not in df.columns:
            result['status'] = '失败'
            result['error_msg'] = f'时间列 {time_column} 不存在'
            return result
        
        df_valid = df[df[column_name].notna()].copy()
        if len(df_valid) == 0:
            result['status'] = '失败'
            result['error_msg'] = '无有效数据'
            return result
        
        df_valid['解析时间'] = pd.to_datetime(df_valid[time_column], format=TIME_FORMAT, errors='coerce')
        df_valid = df_valid[df_valid['解析时间'].notna()].copy()
        
        if len(df_valid) == 0:
            result['status'] = '失败'
            result['error_msg'] = '时间解析失败'
            return result
        
        df_valid = df_valid.sort_values('解析时间').reset_index(drop=True)
        df_valid = df_valid.drop_duplicates(subset=['解析时间'], keep='first').reset_index(drop=True)
        
        max_power_idx = df_valid[column_name].idxmax()
        max_power = df_valid.loc[max_power_idx, column_name]
        max_power_time = df_valid.loc[max_power_idx, '解析时间']
        
        min_power_idx = df_valid[column_name].idxmin()
        min_power = df_valid.loc[min_power_idx, column_name]
        min_power_time = df_valid.loc[min_power_idx, '解析时间']
        
        avg_power = df_valid[column_name].mean()
        
        if first_target_time is not None:
            df_before = df_valid[df_valid['解析时间'] < first_target_time]
            df_after = df_valid[df_valid['解析时间'] >= first_target_time]
            
            if len(df_before) > 0:
                avg_power_before_target = df_before[column_name].mean()
            else:
                avg_power_before_target = None
            
            if len(df_after) > 0:
                avg_power_after_target = df_after[column_name].mean()
            else:
                avg_power_after_target = None
        else:
            avg_power_before_target = None
            avg_power_after_target = None
        
        result['max_power'] = max_power
        result['max_power_time'] = max_power_time
        result['min_power'] = min_power
        result['min_power_time'] = min_power_time
        result['avg_power'] = avg_power
        result['avg_power_before_target'] = avg_power_before_target
        result['avg_power_after_target'] = avg_power_after_target
        
        peak_mask = df_valid[column_name] >= max_power * 0.95
        peak_indices = df_valid[peak_mask].index
        
        if len(peak_indices) > 0:
            peak_start_idx = peak_indices[0]
            peak_end_idx = peak_indices[-1]
            peak_start_time = df_valid.loc[peak_start_idx, '解析时间']
            peak_end_time = df_valid.loc[peak_end_idx, '解析时间']
            
            result['peak_start_time'] = peak_start_time
            result['peak_end_time'] = peak_end_time
            result['peak_duration'] = (peak_end_time - peak_start_time).total_seconds()
        
        return result
        
    except Exception as e:
        result['status'] = '失败'
        result['error_msg'] = str(e)
        return result


def main():
    print("="*70)
    print("📊 温度功率阶段批量分析工具（自动检测所有CH列）")
    print("="*70)
    print(f"📂 文件：{FILE_PATH}")
    print(f"📋 温度列模式：{COLUMN_PATTERN}")
    print(f"⚡ 功率列模式：{POWER_COLUMN_PATTERN}")
    
    # 检查文件是否存在
    if not os.path.exists(FILE_PATH):
        print(f"❌ 文件不存在：{FILE_PATH}")
        return
    
    # 读取Excel文件的所有Sheet
    excel_file = pd.ExcelFile(FILE_PATH)
    all_sheets = excel_file.sheet_names
    print(f"\n📑 发现 {len(all_sheets)} 个Sheet：")
    for i, sheet in enumerate(all_sheets, 1):
        print(f"   {i}. {sheet}")
    
    # 筛选要处理的Sheet
    if SHEETS_TO_PROCESS is None:
        sheets_to_process = all_sheets
    else:
        sheets_to_process = [s for s in SHEETS_TO_PROCESS if s in all_sheets]
    
    print(f"\n🎯 将处理 {len(sheets_to_process)} 个Sheet")
    print("="*70)
    
    # 存储所有结果
    all_results = []
    all_power_results = []
    
    # 遍历处理
    for sheet_name in sheets_to_process:
        print(f"\n📄 处理Sheet：{sheet_name}")
        
        # 解析温度参数
        temp_origin, temp_target = parse_temperature_from_sheet_name(sheet_name)
        direction = detect_temperature_direction(sheet_name)
        
        if temp_origin is not None and temp_target is not None:
            print(f"   🌡️ 解析温度参数：{temp_origin}℃ → {temp_target}℃ ({direction})")
        
        try:
            df = pd.read_excel(FILE_PATH, sheet_name=sheet_name, engine='openpyxl')
            print(f"   ✅ 读取成功，{len(df)} 行数据")
            
            # 如果无法从sheet名称解析温度，尝试从SV列推断
            if temp_origin is None or temp_target is None:
                sv_origin, sv_target = infer_target_temperature_from_sv(df)
                if sv_origin is not None and sv_target is not None:
                    temp_origin = sv_origin
                    temp_target = sv_target
                    print(f"   🌡️ 从SV列推断温度参数：{temp_origin}℃ → {temp_target}℃ ({direction})")
                else:
                    temp_origin = DEFAULT_TEMP_ORIGIN
                    temp_target = DEFAULT_TEMP_TARGET
                    print(f"   ⚠️ 无法从SV列推断温度参数，使用默认值：{temp_origin}℃ → {temp_target}℃")
            
            # 【自动检测】获取所有匹配的温度列
            available_cols = get_temperature_columns(df, COLUMN_PATTERN)
            print(f"   📊 检测到温度列：{available_cols}")
            
            if len(available_cols) == 0:
                print(f"   ⚠️ 没有找到匹配的温度列")
                continue
            
            # 参数配置
            params = {
                'TEMP_ORIGIN': temp_origin,
                'TEMP_TARGET': temp_target,
                'TEMP_TOLERANCE': TEMP_TOLERANCE,
                'TEMP_TOLERANCE_STRICT': TEMP_TOLERANCE_STRICT,
                'OVERSHOOT_MIN_DETECT': OVERSHOOT_MIN_DETECT,
                'N_STABLE': N_STABLE,
                'DIRECTION': direction
            }
            
            temperature_results = {}
            
            # 分析每个温度列
            for col in available_cols:
                print(f"   🔍 分析温度列：{col}")
                result = analyze_temperature_data(df, col, '时间戳', params)
                result['sheet'] = sheet_name
                result['column'] = col
                result['temp_origin'] = temp_origin
                result['temp_target'] = temp_target
                result['direction'] = direction
                all_results.append(result)
                temperature_results[col] = result
                
                if result['status'] == '成功':
                    duration_label = get_duration_label(direction)
                    print(f"      ✅ {duration_label}：{result['cooling_duration']:.1f}s, 超调：{'有' if result['has_overshoot'] else '无'}")
                else:
                    print(f"      ❌ {result['error_msg']}")
            
            # 【自动检测】获取所有匹配的功率列
            power_cols = get_power_columns(df, POWER_COLUMN_PATTERN)
            print(f"   ⚡ 检测到功率列：{power_cols}")
            
            # 分析每个功率列
            for col in power_cols:
                print(f"   🔍 分析功率列：{col}")
                
                ch_num_match = re.match(r'^CH(\d+)输出1\(\%\)$', col)
                first_target_time = None
                
                if ch_num_match:
                    ch_num = ch_num_match.group(1)
                    temp_col_name = f'CH{ch_num}(℃)'
                    if temp_col_name in temperature_results:
                        temp_result = temperature_results[temp_col_name]
                        if temp_result['status'] == '成功':
                            first_target_time = temp_result['first_target_time']
                
                power_result = analyze_power_data(df, col, '时间戳', first_target_time)
                power_result['sheet'] = sheet_name
                power_result['column'] = col
                power_result['direction'] = direction
                all_power_results.append(power_result)
                
                if power_result['status'] == '成功':
                    msg = f"      ✅ 最大功率：{power_result['max_power']:.1f}%, 平均功率：{power_result['avg_power']:.1f}%"
                    if power_result['avg_power_before_target'] is not None:
                        msg += f", 目标前平均功率：{power_result['avg_power_before_target']:.1f}%"
                    if power_result['avg_power_after_target'] is not None:
                        msg += f", 目标后平均功率：{power_result['avg_power_after_target']:.1f}%"
                    print(msg)
                else:
                    print(f"      ❌ {power_result['error_msg']}")
            
        except Exception as e:
            print(f"   ❌ 处理失败：{str(e)}")
    
    # 生成汇总报告
    print("\n" + "="*70)
    print("📊 生成汇总报告...")
    
    # 创建汇总DataFrame
    summary_rows = []
    for r in all_results:
        row = {
            'Sheet': r.get('sheet', ''),
            '方向': r.get('direction', ''),
            '原始温度(℃)': r.get('temp_origin', ''),
            '目标温度(℃)': r.get('temp_target', ''),
            '温度列': r.get('column', ''),
            '状态': r.get('status', ''),
            '开始时间': r.get('start_time', ''),
            '开始温度(℃)': round(r.get('start_temp', 0), 3) if r.get('start_temp') is not None else None,
            '首次到达目标时间': r.get('first_target_time', ''),
            '首次到达温度(℃)': round(r.get('first_target_temp', 0), 3) if r.get('first_target_temp') is not None else None,
            '升温/降温耗时(秒)': round(r.get('cooling_duration', 0), 1) if r.get('cooling_duration') is not None else None,
            '超调峰值温度(℃)': round(r.get('overshoot_temp', 0), 3) if r.get('overshoot_temp') is not None else None,
            '超调峰值时间': r.get('overshoot_time', ''),
            '是否有超调': '是' if r.get('has_overshoot') else '否',
            '保温开始时间': r.get('insulation_start_time', ''),
            '保温开始温度(℃)': round(r.get('insulation_start_temp', 0), 3) if r.get('insulation_start_temp') is not None else None,
            '到达目标到保温开始(秒)': round(r.get('insulation_duration', 0), 1) if r.get('insulation_duration') is not None else None,
            '稳定确认时间': r.get('stable_time', ''),
            '稳定确认温度(℃)': round(r.get('stable_temp', 0), 3) if r.get('stable_temp') is not None else None,
            '保温到稳定耗时(秒)': round(r.get('stable_duration', 0), 1) if r.get('stable_duration') is not None else None,
            '错误信息': r.get('error_msg', '')
        }
        summary_rows.append(row)
    
    summary_df = pd.DataFrame(summary_rows)
    
    # 统计汇总
    total_tests = len(all_results)
    success_count = len([r for r in all_results if r['status'] == '成功'])
    overshoot_count = len([r for r in all_results if r.get('has_overshoot') and r['status'] == '成功'])
    
    print(f"\n📈 统计结果：")
    print(f"   总测试数：{total_tests}")
    print(f"   成功数：{success_count}")
    print(f"   失败数：{total_tests - success_count}")
    print(f"   有超调：{overshoot_count}")
    print(f"   无超调：{success_count - overshoot_count}")
    
    # 按Sheet统计
    if success_count > 0:
        sheet_stats = summary_df[summary_df['状态'] == '成功'].groupby('Sheet').agg({
            '温度列': 'count',
            '升温/降温耗时(秒)': 'mean',
            '是否有超调': lambda x: (x == '是').sum()
        }).reset_index()
        sheet_stats.columns = ['Sheet', '成功列数', '平均升温/降温耗时(秒)', '有超调数']
    else:
        sheet_stats = pd.DataFrame(columns=['Sheet', '成功列数', '平均升温/降温耗时(秒)', '有超调数'])
    
    # 按温度列统计
    if success_count > 0:
        col_stats = summary_df[summary_df['状态'] == '成功'].groupby('温度列').agg({
            'Sheet': 'count',
            '升温/降温耗时(秒)': 'mean',
            '是否有超调': lambda x: (x == '是').sum()
        }).reset_index()
        col_stats.columns = ['温度列', '测试数', '平均升温/降温耗时(秒)', '有超调数']
    else:
        col_stats = pd.DataFrame(columns=['温度列', '测试数', '平均升温/降温耗时(秒)', '有超调数'])
    
    # 功率分析结果汇总
    power_summary_rows = []
    for r in all_power_results:
        row = {
            'Sheet': r.get('sheet', ''),
            '方向': r.get('direction', ''),
            '功率列': r.get('column', ''),
            '状态': r.get('status', ''),
            '最大功率(%)': round(r.get('max_power', 0), 1) if r.get('max_power') is not None else None,
            '最大功率时间': r.get('max_power_time', ''),
            '最小功率(%)': round(r.get('min_power', 0), 1) if r.get('min_power') is not None else None,
            '最小功率时间': r.get('min_power_time', ''),
            '平均功率(%)': round(r.get('avg_power', 0), 1) if r.get('avg_power') is not None else None,
            '首次到达目标前平均功率(%)': round(r.get('avg_power_before_target', 0), 1) if r.get('avg_power_before_target') is not None else None,
            '首次到达目标后平均功率(%)': round(r.get('avg_power_after_target', 0), 1) if r.get('avg_power_after_target') is not None else None,
            '峰值开始时间': r.get('peak_start_time', ''),
            '峰值结束时间': r.get('peak_end_time', ''),
            '峰值持续时间(秒)': round(r.get('peak_duration', 0), 1) if r.get('peak_duration') is not None else None,
            '错误信息': r.get('error_msg', '')
        }
        power_summary_rows.append(row)
    
    power_summary_df = pd.DataFrame(power_summary_rows)
    
    # 按功率列统计
    power_success_count = len([r for r in all_power_results if r['status'] == '成功'])
    if power_success_count > 0:
        power_col_stats = power_summary_df[power_summary_df['状态'] == '成功'].groupby('功率列').agg({
            'Sheet': 'count',
            '最大功率(%)': 'mean',
            '平均功率(%)': 'mean',
            '首次到达目标前平均功率(%)': 'mean',
            '首次到达目标后平均功率(%)': 'mean'
        }).reset_index()
        power_col_stats.columns = ['功率列', '测试数', '平均最大功率(%)', '平均功率(%)', '平均首次到达目标前功率(%)', '平均首次到达目标后功率(%)']
    else:
        power_col_stats = pd.DataFrame(columns=['功率列', '测试数', '平均最大功率(%)', '平均功率(%)', '平均首次到达目标前功率(%)', '平均首次到达目标后功率(%)'])
    
    # 保存报告
    output_path = os.path.join(os.path.dirname(FILE_PATH), REPORT_NAME)
    
    with pd.ExcelWriter(output_path, engine='openpyxl') as writer:
        summary_df.to_excel(writer, sheet_name='温度汇总报告', index=False)
        
        stats_df = pd.DataFrame({
            '指标': ['总测试数', '成功数', '失败数', '有超调', '无超调'],
            '数值': [total_tests, success_count, total_tests - success_count, overshoot_count, success_count - overshoot_count]
        })
        stats_df.to_excel(writer, sheet_name='温度统计摘要', index=False)
        
        if len(sheet_stats) > 0:
            sheet_stats.to_excel(writer, sheet_name='温度Sheet统计', index=False)
        
        if len(col_stats) > 0:
            col_stats.to_excel(writer, sheet_name='温度列统计', index=False)
        
        power_stats_df = pd.DataFrame({
            '指标': ['总功率测试数', '功率测试成功数', '功率测试失败数'],
            '数值': [len(all_power_results), power_success_count, len(all_power_results) - power_success_count]
        })
        power_stats_df.to_excel(writer, sheet_name='功率统计摘要', index=False)
        
        if len(power_summary_df) > 0:
            power_summary_df.to_excel(writer, sheet_name='功率汇总报告', index=False)
        
        if len(power_col_stats) > 0:
            power_col_stats.to_excel(writer, sheet_name='功率列统计', index=False)
    
    print(f"\n💾 报告已保存至：{output_path}")
    print("="*70)
    
    # 显示统计
    if len(sheet_stats) > 0:
        print("\n📊 Sheet统计：")
        print(sheet_stats.to_string(index=False))
    
    if len(col_stats) > 0:
        print("\n📊 温度列统计：")
        print(col_stats.to_string(index=False))
    
    # 打印功率统计
    if len(power_summary_df) > 0:
        print("\n⚡ 功率统计结果：")
        print(f"   总功率测试数：{len(all_power_results)}")
        print(f"   功率测试成功数：{power_success_count}")
        print(f"   功率测试失败数：{len(all_power_results) - power_success_count}")
    
    if len(power_col_stats) > 0:
        print("\n⚡ 功率列统计：")
        print(power_col_stats.to_string(index=False))


if __name__ == '__main__':
    main()
